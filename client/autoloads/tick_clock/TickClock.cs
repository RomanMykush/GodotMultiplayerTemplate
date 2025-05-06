using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class TickClock : Node, IInitializable
{
    public static TickClock Singleton { get; private set; }

    // Signals
    [Signal] public delegate void InterpolationTickUpdatedEventHandler(GodotWrapper<SoftTick> wrapper);
    [Signal] public delegate void ExtrapolationTickUpdatedEventHandler(GodotWrapper<SoftTick> wrapper, float tickDelta);
    [Signal] public delegate void PredictionTickUpdatedEventHandler(GodotWrapper<SoftTick> wrapper, float tickDelta);

    // Exports
    // NOTE: After testing on 150ms/15ms normal deviation 2 was good enough, but 1.5 work great on localhost (Maybe it can even lower but not exactly 1)
    // TODO: It looks like std is always lower then real value. Fix it
    [Export(PropertyHint.Range, "1,5,")] private float SafeTickMargin = 2;

    // Child nodes
    private LatencyCalculator Calculator;
    private TickSynchronizer Synchronizer;
    private SyncPinger Pinger;

    // Other properties
    private float AvarageLatency;
    private float LatencyStd;
    private float Jitter => 3 * LatencyStd; // Set jitter as 99.7% of distribution (or 3 standart deviations)
    private readonly Stopwatch TickUpdateStopwatch = new();
    private float PhysicsProcessDelta;
    private float PreviousBuffer;

    public override void _Ready()
    {
        Singleton = this;

        Calculator = GetNode<LatencyCalculator>("%Calculator");
        Synchronizer = GetNode<TickSynchronizer>("%Synchronizer");
        Pinger = GetNode<SyncPinger>("%Pinger");

        Disable();
        Multiplayer.ServerDisconnected += () => Disable();
    }

    public IEnumerable<JobInfo> ConstructInitJobs()
    {
        // TODO: Remove when LatencyCalculator missing packets problem is solved
        Pinger.ProcessMode = ProcessModeEnum.Always;

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
            ProcessMode = ProcessModeEnum.Always;
            TickUpdateStopwatch.Start();
        };

        return [new(combinedJobs)];
    }

    private SoftTick UpdateServerTick()
    {
        // Getting precise delta
        double processDelta = TickUpdateStopwatch.Elapsed.TotalSeconds;
        TickUpdateStopwatch.Restart();

        var (serverTick, delta) = Synchronizer.UpdateTick((float)processDelta);
        PhysicsProcessDelta += delta;
        return serverTick;
    }

    public override void _Process(double _)
    {
        var serverTick = UpdateServerTick();

        float buffer = Jitter + AvarageLatency + serverTick.TickInterval;

        // Calculate interpolation tick
        var interpolationTick = serverTick.AddDuration(-buffer);
        EmitSignal(SignalName.InterpolationTickUpdated, new GodotWrapper<SoftTick>(interpolationTick));
    }

    public override void _PhysicsProcess(double _)
    {
        var serverTick = UpdateServerTick();

        float buffer = Jitter + AvarageLatency + serverTick.TickInterval * SafeTickMargin;

        // Add elapsed delta time from the last _Process call
        var extrapolationTick = serverTick.AddDuration(-buffer);
        var predictionTick = serverTick.AddDuration(buffer);

        EmitSignal(SignalName.ExtrapolationTickUpdated, new GodotWrapper<SoftTick>(extrapolationTick), PhysicsProcessDelta - buffer + PreviousBuffer);
        EmitSignal(SignalName.PredictionTickUpdated, new GodotWrapper<SoftTick>(predictionTick), PhysicsProcessDelta + buffer - PreviousBuffer);
        PhysicsProcessDelta = 0;
        PreviousBuffer = buffer;
    }

    private void OnLatencyCalculated(float avarage, float std)
    {
        AvarageLatency = avarage;
        LatencyStd = std;
    }

    public void Disable()
    {
        ProcessMode = ProcessModeEnum.Disabled;
        Pinger.ProcessMode = ProcessModeEnum.Disabled;
        TickUpdateStopwatch.Reset();
    }
}
