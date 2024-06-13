using Godot;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public partial class JobHost : Node
{
    public static JobHost Singleton { get; private set; }

    public override void _Ready() =>
        Singleton = this;

    /// <summary> Adds <c>Job</c> as child and runs it. Note that exceptions will be propagated. </summary>
    public async Task RunJob(Job job)
    {
        job.Completed += () => job.QueueFree();
        job.Failed += () => job.QueueFree();
        AddChild(job);
        try
        {
            await job.Run();
        }
        catch (Exception ex)
        {
            // Log error
            Logger.Singleton.Log(LogLevel.Error, $"Scene transition jobs failed with exception: {ex.Message}");
            throw;
        }
    }
}
