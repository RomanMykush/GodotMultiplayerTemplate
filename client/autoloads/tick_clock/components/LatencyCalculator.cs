using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class LatencyCalculator : Node, IInitializable
{
    [Signal] public delegate void LatencyCalculatedEventHandler(float avarage, float std);

    [Export] private uint MinSampleSize = 5;
    [Export] private uint SampleSize = 15;

    private readonly Queue<float> LatencySamples = new(); // Latency samples of Sync message in seconds

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        LatencySamples.Clear();
        // Start populating LatencySamples with data
        var jobs = new Dictionary<Job, float>();
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
                    var awaiter = await DeferredUtils.RunDeferred(() => ToSignal(Network.Singleton, Network.SignalName.MessageReceived));
                    var args = await awaiter;
                    var wrapper = (GodotWrapper<INetworkMessage>)args[0];
                    sync = wrapper.Value as Sync;
                }
                // Store latency
                AppendLatency(sync);
            })
            { SuccessMessage = "Synchronization in process..." };

            jobs.Add(syncReceiveJob, 1);
        }
        // Combine jobs
        var combinedJobs = new ConcurrentJob(jobs) { SuccessMessage = "Synchronization complete" };

        combinedJobs.Completed += () =>
        {
            // Subscribe to sync messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                if (msg.Value is Sync sync)
                    OnSyncReceived(sync);
            };
        };

        return new List<JobInfo>() { new(combinedJobs) };
    }

    private void OnSyncReceived(Sync sync)
    {
        AppendLatency(sync);

        // Calculate numerical characteristics of distribution
        float mathExpectation = LatencySamples.Sum(x => x) / LatencySamples.Count;
        float dispersion = LatencySamples.Sum(x => { float res = x - mathExpectation; return res * res; }) / (LatencySamples.Count - 1);
        float std = Mathf.Sqrt(dispersion);

        EmitSignal(SignalName.LatencyCalculated, mathExpectation, std);
    }

    private void AppendLatency(Sync sync)
    {
        uint currentTime = (uint)Time.GetTicksMsec();

        // Update samples
        float avarageLatency = (currentTime - sync.ClientTime) / 2f; // in milliseconds
        LatencySamples.Enqueue(avarageLatency / 1000);
        if (LatencySamples.Count > SampleSize)
            LatencySamples.Dequeue();
    }
}
