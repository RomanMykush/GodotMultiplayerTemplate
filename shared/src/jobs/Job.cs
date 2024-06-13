using Godot;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public record JobMetric(float ProgressPercent, string Description);

public abstract partial class Job : Node
{
    [Signal] public delegate void UpdatedEventHandler(GodotWrapper<JobMetric> wrapper);
    [Signal] public delegate void CompletedEventHandler();
    [Signal] public delegate void FailedEventHandler();

    public string SuccessMessage = "Job completed";

    /// <summary> Begins <c>Job</c> execution. </summary>
    public abstract Task Run();
}
