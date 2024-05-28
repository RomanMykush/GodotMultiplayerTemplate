using Godot;
using System;

namespace SteampunkDnD.Shared;

public struct Tick
{
    public uint CurrentTick = 1;
    public float TickDuration = 0; // Seconds passed through previous tick
    public uint TickRate = 0; // Ticks in one second
    public readonly float TickInterval => 1.0f / TickRate;

    public Tick(uint tickRate)
    {
        TickRate = tickRate;
    }

    public Tick SetTime(uint tick, float tickDuration)
    {
        CurrentTick = tick;
        TickDuration = tickDuration;
        return this;
    }

    public Tick AddDuration(float duration)
    {
        TickDuration += duration;
        uint deltaTicks = (uint)Mathf.FloorToInt(TickDuration * TickRate);
        CurrentTick += deltaTicks;
        TickDuration %= TickInterval;
        return this;
    }

    public static float GetDuration(Tick start, Tick end)
    {
        if (start.TickRate != end.TickRate)
        {
            Logger.Singleton.Log(LogLevel.Error, "Tried to subtract two instances of Tick with different TickRate value");
            return 0;
        }

        float result = ((float)end.CurrentTick - start.CurrentTick) * start.TickInterval + end.TickDuration - start.TickDuration;
        return result;
    }

    public override readonly string ToString()
    {
        return $"{CurrentTick}:{TickDuration:0.####}";
    }
}
