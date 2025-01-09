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
    [Signal] public delegate void InterpolationTickUpdatedEventHandler(GodotWrapper<Tick> wrapper);
    [Signal] public delegate void ExtrapolationTickUpdatedEventHandler(GodotWrapper<Tick> wrapper, float tickDelta);
    [Signal] public delegate void PredictionTickUpdatedEventHandler(GodotWrapper<Tick> wrapper, float tickDelta);

    // Exports
    [Export(PropertyHint.Range, "1,5,")] private uint SafeTickMargin = 2;

    // Child nodes
    private LatencyCalculator Calculator;
    private TickSynchronizer Synchronizer;
    private SyncPinger Pinger;

    // Other properties
    private float AvarageLatency;
    private float LatencyStd;
    private readonly Stopwatch TickUpdateStopwatch = new();
    private float PhysicsProcessDelta;

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

        return new List<JobInfo>() { new(combinedJobs) };
    }

    private Tick UpdateServerTick()
    {
        // Getting precise delta
        double processDelta = TickUpdateStopwatch.Elapsed.TotalSeconds;
        TickUpdateStopwatch.Restart();

        var (serverTick, delta) = Synchronizer.UpdateTick((float)processDelta);
        PhysicsProcessDelta += delta;
        return serverTick;
    }

    private float CalculateOffsetBuffer(float tickInterval)
    {
        // Set jitter as 99.7% of distribution (or 3 standart deviations)
        float jitter = 3 * LatencyStd;
        return jitter + AvarageLatency + tickInterval * SafeTickMargin;
    }

    public override void _Process(double _)
    {
        var serverTick = UpdateServerTick();

        float buffer = CalculateOffsetBuffer(serverTick.TickInterval);

        // Calculate interpolation tick
        var interpolationTick = serverTick.AddDuration(-buffer);
        EmitSignal(SignalName.InterpolationTickUpdated, new GodotWrapper<Tick>(interpolationTick));
    }

    public override void _PhysicsProcess(double _)
    {
        var serverTick = UpdateServerTick();

        float buffer = CalculateOffsetBuffer(serverTick.TickInterval);

        // Add elapsed delta time from the last _Process call
        var extrapolationTick = serverTick.AddDuration(-buffer);
        var predictionTick = serverTick.AddDuration(buffer);

        EmitSignal(SignalName.ExtrapolationTickUpdated, new GodotWrapper<Tick>(extrapolationTick), PhysicsProcessDelta);
        EmitSignal(SignalName.PredictionTickUpdated, new GodotWrapper<Tick>(predictionTick), PhysicsProcessDelta);
        PhysicsProcessDelta = 0;
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
