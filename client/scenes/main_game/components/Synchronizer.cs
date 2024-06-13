using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class Synchronizer : Node, IInitializable
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

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Disabled;

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
                Network.Singleton.SendPacket(syncInfoRequest, MultiplayerPeer.TransferModeEnum.Reliable);
            });
            // Wait for SyncInfo message to arrive
            while (syncInfo == null)
            {
                var awaiter = await DeferredUtils.RunDeferred(() => ToSignal(Network.Singleton, Network.SignalName.MessageReceived));
                var args = await awaiter;
                var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                syncInfo = wrapper.Value as SyncInfo;
            }
        })
        { SuccessMessage = "Synchronization info received" };

        syncInfoReceiveJob.Completed += () =>
        {
            // Set tick rate
            PredictionTick = new Tick((uint)syncInfo.ServerTicksPerSecond);
        };

        // Start populating LatencySamples with data
        var jobs = new Dictionary<Job, float>() { {syncInfoReceiveJob, MinSampleSize} };
        Sync latestSync = null;
        for (int i = 0; i < MinSampleSize; i++)
        {
            var syncReceiveJob = new AtomicJob(async () =>
            {
                // Send sync message
                DeferredUtils.CallDeferred(() =>
                {
                    var sync = new Sync((uint)Time.GetTicksMsec(), 0);
                    Network.Singleton.SendPacket(sync, MultiplayerPeer.TransferModeEnum.Reliable);
                });
                // Wait for Sync message to arrive
                Sync sync = null;
                while (sync == null)
                {
                    // NOTE: Based on observations, execution of deferred calls starts before execution of signal handlers
                    var awaiter = await DeferredUtils.RunDeferred(() => ToSignal(Network.Singleton, Network.SignalName.MessageReceived));
                    var args = await awaiter;
                    var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                    sync = wrapper.Value as Sync;
                }
                // Save latency to collection
                uint avarageLatency = ((uint)Time.GetTicksMsec() - sync.ClientTime) / 2;
                LatencySamples.Enqueue(avarageLatency);
                // Set as latest Sync message
                latestSync = sync;
            })
            { SuccessMessage = "Synchronization in process..." };

            jobs.Add(syncReceiveJob, 1);
        }

        // Combine jobs
        var combinedJobs = new ConcurrentJob(jobs) { SuccessMessage = "Synchronization complete" };
        // Finish initialization
        combinedJobs.Completed += () =>
        {
            // Synchronize prediction tick
            var preferredTick = CalculatePreferredTick(latestSync.ServerTick);
            CatchUpPrediction(preferredTick);

            ProcessMode = ProcessModeEnum.Always;

            // Start timer
            GetNode<Timer>("%SyncTimer").Start();

            // Subscribe to sync messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                if (msg.Value is Sync sync)
                    OnSyncReceived(sync);
            };
        };

        return new List<JobInfo>() { new(combinedJobs) };
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

        // Synchronize prediction tick
        var preferredTick = CalculatePreferredTick(sync.ServerTick);
        CatchUpPrediction(preferredTick);
    }

    private Tick CalculatePreferredTick(uint serverTick)
    {
        // Calculate numerical characteristics of distribution in ms
        float mathExp = (float)LatencySamples.Sum(x => x) / LatencySamples.Count;
        float disp = LatencySamples.Sum(x => { float res = x - mathExp; return res * res; }) / (LatencySamples.Count - 1);
        float std = Mathf.Sqrt(disp);

        // Set jitter as 99.7% of distribution (or 3 standart deviations)
        float jitter = 3 * std;

        // Calculate optional buffer
        uint tickBuffer = Math.Max(MinimumTickBuffer,
            (uint)Mathf.Ceil(jitter / 1000f * PredictionTick.TickRate)); // Buffer for input messages to reach server

        return new Tick(PredictionTick.TickRate) { CurrentTick = serverTick + tickBuffer }
            .AddDuration(mathExp / 1000f);
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
