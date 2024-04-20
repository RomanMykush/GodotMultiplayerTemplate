using Godot;
using System;

namespace SteampunkDnD.Shared;

public record JobMetric(float ProgressPercent, string Description);

public abstract partial class JobObserver : Node
{
    [Signal] public delegate void UpdatedEventHandler(GodotWrapper<JobMetric> jobsMetric);
    [Signal] public delegate void CompletedEventHandler();
    [Signal] public delegate void FailedEventHandler();

    public JobObserver()
    {
        Completed += () => { QueueFree(); };
        Failed += () => { QueueFree(); };
    }
}
