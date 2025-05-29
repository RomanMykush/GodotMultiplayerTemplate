using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Tests;

public partial class TickSynchronizerImitation : Node
{
    // Exports
    [Export] private Curve SyncCurve;
    [Export] private uint TolerableTickDifference = 3;

    public uint CurrentTick { get; private set; } // Current predicted server tick
    public float PreferredTick { get; private set; }
    private float AvarageLatency;
    private float AccumulatedDeviation;
    private float CatchUpTimeScale;

    public override void _Ready()
    {
        if (SyncCurve == null)
        {
            Logger.Singleton.Log(LogLevel.Error, "Curve resource for synchronization wasn't set");
            return;
        }
        SyncCurve.Bake();
    }

    public uint UpdateTick(float delta)
    {
        CurrentTick++;
        PreferredTick++;

        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float deviation = delta * CatchUpTimeScale - physicsInterval;
        AccumulatedDeviation += deviation;

        if (Math.Abs(AccumulatedDeviation) > physicsInterval)
            CurrentTick = (uint)(CurrentTick + (int)(AccumulatedDeviation / physicsInterval));
        AccumulatedDeviation %= physicsInterval;

        UpdateCatchUp();

        return CurrentTick;
    }

    private void UpdateCatchUp()
    {
        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        float errorTicksDelta = PreferredTick - CurrentTick + (AccumulatedDeviation / physicsInterval);
        if (Math.Abs(errorTicksDelta) > TolerableTickDifference)
        {
            // Hard catch up
            CurrentTick = (uint)PreferredTick;
            AccumulatedDeviation = 0;
            CatchUpTimeScale = 1f;
            return;
        }
        // Soft catch up
        float offset = Math.Abs(errorTicksDelta / TolerableTickDifference);
        float y = SyncCurve.SampleBaked(offset);

        if (errorTicksDelta >= 0)
            y = -y;

        CatchUpTimeScale = 1f + y;
    }

    private void OnLatencyCalculated(float avarage, float _) => AvarageLatency = avarage;

    public void OnSyncReceived(Sync sync)
    {
        float physicsInterval = 1f / Engine.PhysicsTicksPerSecond;
        // Perform Min() on duration for cases when server performance can't keep up with tickrate
        float tickDuration = Math.Min(sync.ServerTickDuration, physicsInterval);
        float shiftInTicks = (tickDuration + AvarageLatency) / physicsInterval;
        PreferredTick = sync.ServerTick + shiftInTicks;

        UpdateCatchUp();
    }

    public void ClearState()
    {
        CurrentTick = 0;
        PreferredTick = 0;
        AvarageLatency = 0;
        AccumulatedDeviation = 0;
        CatchUpTimeScale = 0;
    }
}
