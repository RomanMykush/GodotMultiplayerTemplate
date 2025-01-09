using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SteampunkDnD.Client;

public partial class TickSynchronizer : Node, IInitializable
{
    // Signals
    [Signal] public delegate void HardCatchUpHappenedEventHandler();

    // Exports
    [Export] private Curve SyncCurve;
    [Export] private uint TolerableTickDifference = 3;

    // Other properties
    private float AvarageLatency;
    private Tick CurrentServerTick;
    private float CatchUpTimeScale;

    public override void _Ready()
    {
        if (SyncCurve == null)
        {
            Logger.Singleton.Log(LogLevel.Error, "Curve resource for synchronization wasn't set");
            return;
        }
        SyncCurve.Bake();
    }

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        // Wait for SyncInfo
        SyncInfo syncInfo = null;
        var syncInfoReceiveJob = new AtomicJob(async () =>
        {
            // Send sync info request
            DeferredUtils.CallDeferred(() =>
            {
                var syncInfoRequest = new SyncInfoRequest();
                Network.Singleton.SendMessage(syncInfoRequest, MultiplayerPeer.TransferModeEnum.Reliable);
            });
            // Wait for SyncInfo message to arrive
            SignalAwaiter awaiter = null;
            while (syncInfo == null)
            {
                if (SynchronizationContext.Current == AppManager.MainThreadSyncContext)
                    awaiter = ToSignal(Network.Singleton, Network.SignalName.MessageReceived);
                else awaiter = await DeferredUtils.RunDeferred(() => ToSignal(Network.Singleton, Network.SignalName.MessageReceived));

                var args = await awaiter;
                var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                syncInfo = wrapper.Value as SyncInfo;
            }
        })
        { SuccessMessage = "Synchronization info received" };

        syncInfoReceiveJob.Completed += () =>
        {
            // Set tick rate
            CurrentServerTick = new Tick((uint)syncInfo.ServerTicksPerSecond);

            // Subscribe to Sync and SyncInfo messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                switch (msg.Value)
                {
                    case SyncInfo syncInfo:
                        OnSyncInfoReceived(syncInfo);
                        break;
                    case Sync sync:
                        OnSyncReceived(sync);
                        break;
                }
            };
        };
        return new List<JobInfo>() { new(syncInfoReceiveJob) };
    }

    /// <summary> Updates <c>CurrentServerTick</c> and return it with time-scaled delta time. </summary>
    public (Tick, float) UpdateTick(float delta)
    {
        float finalDelta = delta * CatchUpTimeScale;
        CurrentServerTick = CurrentServerTick.AddDuration(finalDelta);
        return (CurrentServerTick, finalDelta);
    }

    private void OnSyncInfoReceived(SyncInfo syncInfo) =>
        CurrentServerTick = new Tick((uint)syncInfo.ServerTicksPerSecond) { CurrentTick = CurrentServerTick.CurrentTick };

    private void OnLatencyCalculated(float avarage, float _) => AvarageLatency = avarage;

    private void OnSyncReceived(Sync sync)
    {
        var preferredTick = new Tick(CurrentServerTick.TickRate) { CurrentTick = sync.ServerTick }
            .AddDuration(AvarageLatency);

        float errorTicksDelta = Tick.GetDuration(preferredTick, CurrentServerTick) * CurrentServerTick.TickRate;
        if (Math.Abs(errorTicksDelta) > TolerableTickDifference)
        {
            // Hard catch up
            CurrentServerTick = preferredTick;
            CatchUpTimeScale = 1f;
            EmitSignal(SignalName.HardCatchUpHappened);
            return;
        }
        // Soft catch up
        float offset = Math.Abs(errorTicksDelta / TolerableTickDifference);
        float y = SyncCurve.SampleBaked(offset);

        if (errorTicksDelta >= 0)
            y = -y;

        CatchUpTimeScale = 1f + y;
    }
}
