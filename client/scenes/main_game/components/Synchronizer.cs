using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class Synchronizer : Node
{
    // Signals
    [Signal] public delegate void TickUpdatedEventHandler(int currentTick, float tickDuration);
    [Signal] public delegate void HardCatchUpHappenedEventHandler();

    // Exports
    [Export] private uint MinSampleSize = 5;
    [Export] private uint SampleSize = 15;
    [Export] private Curve SyncCurve;
    [Export] private uint TolerableTickDifference = 3;
    [Export] private uint MinimumTickBuffer = 2;

    // Other properties
    public Tick PredictionTick { get; private set; }
    public float PredictionTimeScale { get; private set; }
    private readonly Queue<uint> LatencySamples = new(); // Latency samples of Sync message in milliseconds

    public async override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Disabled;

        if (SyncCurve == null)
        {
            Logger.Singleton.Log(LogLevel.Error, "Curve resource for synchronization wasn't set");
            return;
        }
        SyncCurve.Bake();

        var awaiter = ToSignal(Network.Singleton, Network.SignalName.MessageReceived);

        await Task.Run(async () =>
        {
            // Await SyncInfo message from server
            SyncInfo syncInfo;
            while (true)
            {
                var args = await awaiter;
                var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                syncInfo = wrapper.Value as SyncInfo;
                if (syncInfo != null)
                    break;
                awaiter = ToSignal(Network.Singleton, Network.SignalName.MessageReceived);
            }

            // Set tick rate
            PredictionTick = new Tick((uint)syncInfo.ServerTicksPerSecond);

            // Populate LatencySamples with data
            for (int i = 0; i < MinSampleSize; i++)
            {
                var sync = new Sync((uint)Time.GetTicksMsec(), 0);
                Network.Singleton.SendPacket(sync, MultiplayerPeer.TransferModeEnum.Reliable);
            }
            // Wait for data to arrive
            int syncReceived = 0;
            while (syncReceived < MinSampleSize)
            {
                var args = await ToSignal(Network.Singleton, Network.SignalName.MessageReceived);
                var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                var sync = wrapper.Value as Sync;
                if (sync == null)
                    continue;

                syncReceived++;
                if (syncReceived == MinSampleSize)
                {
                    OnSyncReceived(sync);
                    ProcessMode = ProcessModeEnum.Always;
                    continue;
                }

                uint avarageLatency = ((uint)Time.GetTicksMsec() - sync.ClientTime) / 2;
                LatencySamples.Enqueue(avarageLatency);
            }

            // Start timer
            GetNode<Timer>("%SyncTimer").Start();

            // Subscribe to sync messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                if (msg.Value is Sync sync)
                    OnSyncReceived(sync);
            };
        });
    }

    public override void _Process(double delta)
    {
        PredictionTick = PredictionTick.AddDuration((float)delta * PredictionTimeScale);
        EmitSignal(SignalName.TickUpdated, PredictionTick.CurrentTick, PredictionTick.TickDuration);
    }

    private void SendSync()
    {
        var sync = new Sync((uint)Time.GetTicksMsec(), 0);
        Network.Singleton.SendPacket(sync);
    }

    private void OnSyncReceived(Sync sync)
    {
        uint currentTime = (uint)Time.GetTicksMsec();

        // Update samples
        uint avarageLatency = (currentTime - sync.ClientTime) / 2;
        LatencySamples.Enqueue(avarageLatency);
        if (LatencySamples.Count > SampleSize)
            LatencySamples.Dequeue();

        // Calculate numerical characteristics of distribution in ms
        float mathExp = (float)LatencySamples.Sum(x => x) / LatencySamples.Count;
        float disp = LatencySamples.Sum(x => { float res = x - mathExp; return res * res; }) / (LatencySamples.Count - 1);
        float std = Mathf.Sqrt(disp);

        // Set jitter as 99.7% of distribution (or 3 standart deviations)
        float jitter = 3 * std;

        // Synchronize prediction tick
        uint tickBuffer = Math.Max(MinimumTickBuffer,
            (uint)Mathf.Ceil(jitter / 1000f * PredictionTick.TickRate)); // Buffer for input messages to reach server

        var preferredTick = new Tick(PredictionTick.TickRate) { CurrentTick = sync.ServerTick + tickBuffer }
            .AddDuration(mathExp / 1000f);

        CatchUpPrediction(preferredTick);
    }

    private void CatchUpPrediction(Tick preferredTick)
    {
        float errorTicksDelta = Tick.GetDuration(preferredTick, PredictionTick) * PredictionTick.TickRate;
        if (Math.Abs(errorTicksDelta) > TolerableTickDifference)
        {
            // Hard catch up
            PredictionTick = preferredTick;
            PredictionTimeScale = 1f;
            EmitSignal(SignalName.HardCatchUpHappened);
            return;
        }
        // Soft catch up
        float offset = Math.Abs(errorTicksDelta / TolerableTickDifference);
        float y = SyncCurve.SampleBaked(offset);

        if (errorTicksDelta >= 0)
            y = -y;

        PredictionTimeScale = 1f + y;
    }
}
