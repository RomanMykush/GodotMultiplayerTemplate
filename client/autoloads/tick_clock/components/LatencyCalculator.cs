using Godot;
using GodotMultiplayerTemplate.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GodotMultiplayerTemplate.Client;

public partial class LatencyCalculator : Node, IInitializable
{
    [Signal] public delegate void LatencyCalculatedEventHandler(float avarage, float std);

    [Export] private uint MinSampleSize = 5;
    [Export] private uint SampleSize = 30;

    private readonly Queue<float> LatencySamples = new(); // Latency samples of sync message in seconds

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        LatencySamples.Clear();
        // Start populating LatencySamples with data
        // TODO: Fix this, sometimes signals of incoming sync packets are missed. Temporary solution is turning on SyncPinger
        var jobs = new Dictionary<Job, float>();
        for (int i = 0; i < MinSampleSize; i++)
        {
            var syncReceiveJob = new AtomicJob(async () =>
            {
                // Send sync message
                DeferredUtils.CallDeferred(() =>
                {
                    var sync = new SyncRequest((uint)Time.GetTicksMsec());
                    Network.Singleton.SendMessage(sync, MultiplayerPeer.TransferModeEnum.Reliable);
                });
                // Wait for sync message to arrive
                Sync sync = null;
                SignalAwaiter awaiter = null;
                while (sync == null)
                {
                    if (SynchronizationContext.Current == AppManager.MainThreadSyncContext)
                        awaiter = ToSignal(Network.Singleton, Network.SignalName.MessageReceived);
                    else awaiter = await DeferredUtils.RunDeferred(() => ToSignal(Network.Singleton, Network.SignalName.MessageReceived));

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
            CalculateLatency();
            // Subscribe to sync messages
            Network.Singleton.MessageReceived += (msg) =>
            {
                if (msg.Value is Sync sync)
                    OnSyncReceived(sync);
            };
        };

        return [new(combinedJobs)];
    }

    private void OnSyncReceived(Sync sync)
    {
        AppendLatency(sync);
        CalculateLatency();
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

    private void CalculateLatency()
    {
        // Calculate numerical characteristics of distribution
        float avarage = LatencySamples.Sum(x => x) / LatencySamples.Count;
        float dispersion = LatencySamples.Sum(x => { float res = x - avarage; return res * res; }) / (LatencySamples.Count - 1);
        float std = Mathf.Sqrt(dispersion);

        EmitSignal(SignalName.LatencyCalculated, avarage, std);
    }
}
