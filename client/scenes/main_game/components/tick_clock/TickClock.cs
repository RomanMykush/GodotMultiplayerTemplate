using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class TickClock : Node, IInitializable
{
    // Signals
    [Signal] public delegate void InterpolationTickUpdatedEventHandler(int currentTick, float tickDuration);
    [Signal] public delegate void ExtrapolationTickUpdatedEventHandler(int currentTick, float tickDuration);
    [Signal] public delegate void PredictionTickUpdatedEventHandler(int currentTick, float tickDuration);

    // Child nodes
    private SyncPinger Pinger;
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
        Pinger = GetNode<SyncPinger>("%Pinger");
        Calculator = GetNode<LatencyCalculator>("%Calculator");
        Synchronizer = GetNode<TickSynchronizer>("%Synchronizer");

        ProcessMode = ProcessModeEnum.Disabled;
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
        combinedJobs.Completed += () =>
        {
            Pinger.Start();
            ProcessMode = ProcessModeEnum.Always;
        };

        return new List<JobInfo>() { new(combinedJobs) };
    }

    public override void _Process(double delta)
    {
        var serverTick = Synchronizer.UpdateTick((float)delta);

        // Set jitter as 99.7% of distribution (or 3 standart deviations)
        float jitter = 3 * LatencyStd;

        // Calculate optional buffer
        uint tickBuffer = (uint)Mathf.Ceil(jitter * serverTick.TickRate);

        // Calculate interpolation tick
        var interpolationTick = (serverTick with { CurrentTick = serverTick.CurrentTick - tickBuffer })
            .AddDuration(-AvarageLatency);
        EmitSignal(SignalName.InterpolationTickUpdated, interpolationTick.CurrentTick, interpolationTick.TickDuration);

        ExtrapolationTick = interpolationTick;

        // Calculate prediction tick
        PredictionTick = (serverTick with { CurrentTick = serverTick.CurrentTick + tickBuffer })
            .AddDuration(AvarageLatency);

        LastProcessTimestamp = (uint)Time.GetTicksMsec();
    }

    public override void _PhysicsProcess(double _)
    {
        uint currentTime = (uint)Time.GetTicksMsec();
        float delta = (currentTime - LastProcessTimestamp) / 1000f;

        // Add elapsed delta time from the last _Process call
        ExtrapolationTick = ExtrapolationTick.AddDuration(delta);
        PredictionTick = PredictionTick.AddDuration(delta);

        EmitSignal(SignalName.ExtrapolationTickUpdated, ExtrapolationTick.CurrentTick, ExtrapolationTick.TickDuration);
        EmitSignal(SignalName.PredictionTickUpdated, PredictionTick.CurrentTick, PredictionTick.TickDuration);
        LastProcessTimestamp = (uint)Time.GetTicksMsec();
    }

    public void OnLatencyCalculated(float avarage, float std)
    {
        AvarageLatency = avarage;
        LatencyStd = std;
    }
}
