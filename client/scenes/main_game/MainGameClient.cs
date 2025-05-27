using Godot;
using GodotMultiplayerTemplate.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Client;

public partial class MainGameClient : Node, IGameMode
{
    public string Address { get; protected set; }
    public int Port { get; protected set; }

    public void SetupClient(string address, int port)
    {
        Address = address;
        Port = port;
    }

    public virtual async Task<PreInitResult> PreInitialize()
    {
        var result = await Network.Singleton.Connect(Address, Port);
        return new PreInitResult(result, "Failed to connect to server");
    }

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        // TODO: Add more items to initialize
        var syncInitJobs = TickClock.Singleton.ConstructInitJobs();

        // Add jobs with weights to dictionary
        var weightedJobs = syncInitJobs.ToDictionary(ji => ji.Job, _ => 1f);

        // Combine jobs
        var combinedJobs = new ParallelJob(weightedJobs);
        return new List<JobInfo>() { new LoadingInfo(combinedJobs) };
    }

    public void CleanUp() => TickClock.Singleton.Disable();
}
