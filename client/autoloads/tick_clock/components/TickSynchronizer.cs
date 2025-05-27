using Godot;
using GodotMultiplayerTemplate.Shared;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GodotMultiplayerTemplate.Client;

public partial class TickSynchronizer : Node, IInitializable
{
    // Signals
    [Signal] public delegate void HardCatchUpHappenedEventHandler();

    // Exports
    [Export] private Curve SyncCurve;
    [Export] private uint TolerableTickDifference = 3;

    // Other properties
    public uint CurrentTick { get; private set; } // Current predicted server tick
    private float PreferredTick;
    private float AvarageLatency;
    private float AccumulatedDeviation;
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
            Engine.PhysicsTicksPerSecond = syncInfo.ServerTicksPerSecond;

            // Subscribe to sync and sync info messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                switch (msg.Value)
                {
                    case SyncInfo syncInfo:
                        Engine.PhysicsTicksPerSecond = syncInfo.ServerTicksPerSecond;
                        break;
                    case Sync sync:
                        OnSyncReceived(sync);
                        break;
                }
            };
        };
        return [new(syncInfoReceiveJob)];
    }

    /// <summary> Updates <c>CurrentServerTick</c> and return it with time-scaled delta time. </summary>
    public uint UpdateTick(float delta)
    {
        CurrentTick++;
        PreferredTick++;

        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float deviation = delta * CatchUpTimeScale - physicsInterval;
        AccumulatedDeviation += deviation;

        if (Math.Abs(AccumulatedDeviation) > physicsInterval)
            CurrentTick = (uint)(CurrentTick + (int)(AccumulatedDeviation / physicsInterval));
        AccumulatedDeviation %= physicsInterval;

        UpdateCatchUp();

        return CurrentTick;
    }

    private void UpdateCatchUp()
    {
        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float errorTicksDelta = PreferredTick - CurrentTick + (AccumulatedDeviation / physicsInterval);
        if (Math.Abs(errorTicksDelta) > TolerableTickDifference)
        {
            // Hard catch up
            CurrentTick = (uint)PreferredTick;
            AccumulatedDeviation = 0;
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

    private void OnLatencyCalculated(float avarage, float _) => AvarageLatency = avarage;

    private void OnSyncReceived(Sync sync)
    {
        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        // Perform Min() on duration for cases when server performance can't keep up with tickrate
        float tickDuration = Math.Min(sync.ServerTickDuration, physicsInterval);
        float shiftInTicks = (tickDuration + AvarageLatency) / physicsInterval;
        PreferredTick = sync.ServerTick + shiftInTicks;

        UpdateCatchUp();
    }
}
