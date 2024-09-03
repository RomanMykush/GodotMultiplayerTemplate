using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class TickClock : Node, IInitializable
{
    public static TickClock Singleton { get; private set; }

    // Signals
    [Signal] public delegate void InterpolationTickUpdatedEventHandler(GodotWrapper<Tick> wrapper);
    [Signal] public delegate void ExtrapolationTickUpdatedEventHandler(GodotWrapper<Tick> wrapper);
    [Signal] public delegate void PredictionTickUpdatedEventHandler(GodotWrapper<Tick> wrapper);

    // Child nodes
    private LatencyCalculator Calculator;
    private TickSynchronizer Synchronizer;

    // Other properties
    private float AvarageLatency;
    private float LatencyStd;
    private Tick ExtrapolationTick;
    private Tick PredictionTick;
    private uint LastProcessTimestamp;

    public override void _Ready()
    {
        Singleton = this;

        Calculator = GetNode<LatencyCalculator>("%Calculator");
        Synchronizer = GetNode<TickSynchronizer>("%Synchronizer");

        Disable();
        Multiplayer.ServerDisconnected += () => Disable();
    }

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        // Construct jobs
        var syncInitJobs = Synchronizer.ConstructInitJobs();
        var calcInitJobs = Calculator.ConstructInitJobs();

        // Set weight for jobs
        var weightedJobsDicts = new List<Dictionary<Job, float>>
        {
            syncInitJobs.ToDictionary(ji => ji.Job, _ => 1f),
            calcInitJobs.ToDictionary(ji => ji.Job, _ => 1f)
        };

        // Merge jobs
        var weightedJobs = new Dictionary<Job, float>();
        foreach (var dict in weightedJobsDicts)
            foreach (var wJob in dict)
                weightedJobs.Add(wJob.Key, wJob.Value);
        var combinedJobs = new ConcurrentJob(weightedJobs);

        // Start pinging the server after initialization is complete
        combinedJobs.Completed += () => ProcessMode = ProcessModeEnum.Always;

        return new List<JobInfo>() { new(combinedJobs) };
    }

    public override void _Process(double delta)
    {
        var serverTick = Synchronizer.UpdateTick((float)delta);

        // Set jitter as 99.7% of distribution (or 3 standart deviations)
        float jitter = 3 * LatencyStd;

        // Calculate offset buffer
        float buffer = jitter + AvarageLatency + serverTick.TickInterval;

        // Calculate interpolation tick
        var interpolationTick = serverTick.AddDuration(-buffer);
        EmitSignal(SignalName.InterpolationTickUpdated, new GodotWrapper<Tick>(interpolationTick));

        ExtrapolationTick = interpolationTick;

        // Calculate prediction tick
        PredictionTick = serverTick.AddDuration(buffer);

        LastProcessTimestamp = (uint)Time.GetTicksMsec();
    }

    public override void _PhysicsProcess(double _)
    {
        uint currentTime = (uint)Time.GetTicksMsec();
        float delta = (currentTime - LastProcessTimestamp) / 1000f;

        // Add elapsed delta time from the last _Process call
        ExtrapolationTick = ExtrapolationTick.AddDuration(delta);
        PredictionTick = PredictionTick.AddDuration(delta);

        EmitSignal(SignalName.ExtrapolationTickUpdated, new GodotWrapper<Tick>(ExtrapolationTick));
        EmitSignal(SignalName.PredictionTickUpdated, new GodotWrapper<Tick>(PredictionTick));
        LastProcessTimestamp = (uint)Time.GetTicksMsec();
    }

    private void OnLatencyCalculated(float avarage, float std)
    {
        AvarageLatency = avarage;
        LatencyStd = std;
    }

    public void Disable() => ProcessMode = ProcessModeEnum.Disabled;
}
