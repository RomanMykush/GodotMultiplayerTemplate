using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class MainGameClient : Node, ILevel
{
    private Synchronizer TickSync;

    public string Address { get; protected set; }
    public int Port { get; protected set; }

    public void SetupClient(string address, int port)
    {
        Address = address;
        Port = port;
    }

    public override void _Ready()
    {
        TickSync = GetNode<Synchronizer>("%TickSync");
    }

    public virtual async Task<PreInitResult> PreInitialize()
    {
        var result = await Network.Singleton.Connect(Address, Port);
        return new PreInitResult(result, "Failed to connect to server");
    }

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        // TODO: Add more items to initialize
        var syncInitJobs = TickSync.ConstructInitJobs();

        // Add jobs with weights to dictionary
        var weightedJobs = syncInitJobs.ToDictionary(ji => ji.Job, _ => 1f);

        // Combine jobs
        var combinedJobs = new ParallelJob(weightedJobs);
        return new List<JobInfo>() { new LoadingInfo(combinedJobs) };
    }

    public void CleanUp()
    {
        // TODO: Implement clean up
        throw new NotImplementedException();
    }
}
