using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class TickClock : Node, IInitializable
{
    public static TickClock Singleton { get; private set; }

    // Signals
    [Signal] public delegate void InterpolationTickUpdatedEventHandler(uint curretnTick);
    [Signal] public delegate void ExtrapolationTickUpdatedEventHandler(uint curretnTick);
    [Signal] public delegate void PredictionTickUpdatedEventHandler(uint curretnTick);

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
    private int PreviousBufferTicks;

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
        combinedJobs.Completed += () => ProcessMode = ProcessModeEnum.Always;

        return [new(combinedJobs)];
    }

    public override void _Process(double _)
    {
        var serverTick = Synchronizer.CurrentTick;

        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float buffer = Jitter + AvarageLatency + physicsInterval;

        // Prevent flickering of buffer tick number
        int bufferTicks = Mathf.CeilToInt(buffer / physicsInterval);
        if (PreviousBufferTicks - 1.5f < (buffer / physicsInterval) && PreviousBufferTicks > bufferTicks)
            bufferTicks = PreviousBufferTicks;
        else PreviousBufferTicks = bufferTicks;

        uint interpolationTick = (uint)(serverTick - bufferTicks);
        EmitSignal(SignalName.InterpolationTickUpdated, interpolationTick);
    }

    public override void _PhysicsProcess(double delta)
    {
        var serverTick = Synchronizer.UpdateTick((float)delta);

        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float buffer = Jitter + AvarageLatency + physicsInterval * SafeTickMargin;

        // Prevent flickering of buffer tick number
        int bufferTicks = Mathf.CeilToInt(buffer / physicsInterval);
        if (PreviousBufferTicks - 1.5f < (buffer / physicsInterval) && PreviousBufferTicks > bufferTicks)
            bufferTicks = PreviousBufferTicks;
        else PreviousBufferTicks = bufferTicks;

        uint extrapolationTick = (uint)(serverTick - bufferTicks);
        uint predictionTick = (uint)(serverTick + bufferTicks);

        EmitSignal(SignalName.ExtrapolationTickUpdated, extrapolationTick);
        EmitSignal(SignalName.PredictionTickUpdated, predictionTick);
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
    }
}
