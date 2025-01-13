using Godot;
using System;

namespace SteampunkDnD.Shared;

public struct Tick
{
    public uint TickCount = 1;
    public float TickDuration = 0; // Seconds passed through previous tick
    public uint TickRate = 0; // Ticks per second
    public readonly float TickInterval => 1.0f / TickRate;
    public const float Epsilon = 1e-6f;

    public Tick(uint tickRate)
    {
        TickRate = tickRate;
    }

    public Tick AddDuration(float duration)
    {
        float newTickDuration = TickDuration + duration;
        uint newTickCount;
        if (newTickDuration >= 0)
        {
            uint deltaTicks = (uint)Mathf.FloorToInt(newTickDuration * TickRate);
            newTickCount = TickCount + deltaTicks;
            // Adding epsilon to avoid rounding error
            newTickDuration += Epsilon;
            newTickDuration %= TickInterval;
            newTickDuration -= Epsilon;
            newTickDuration = Math.Max(newTickDuration, 0);
        }
        else
        {
            uint deltaTicks = (uint)Mathf.CeilToInt(-newTickDuration * TickRate);
            newTickCount = TickCount - deltaTicks;
            // Adding epsilon to avoid rounding error
            newTickDuration += Epsilon;
            newTickDuration %= TickInterval;
            newTickDuration -= Epsilon;
            newTickDuration += TickInterval;
            newTickDuration = Math.Max(newTickDuration, 0);
        }
        return new Tick(TickRate) { TickCount = newTickCount, TickDuration = newTickDuration };
    }

    public static float GetDuration(Tick start, Tick end)
    {
        if (start.TickRate != end.TickRate)
        {
            Logger.Singleton.Log(LogLevel.Error, "Tried to subtract two instances of Tick with different TickRate value");
            return 0;
        }

        float result = ((float)end.TickCount - start.TickCount) * start.TickInterval + end.TickDuration - start.TickDuration;
        return result;
    }

    public override readonly bool Equals(object obj) =>
        obj != null && GetType() == obj.GetType() && this == (Tick)obj;

    public override readonly int GetHashCode() =>
        (TickCount, TickDuration, TickRate).GetHashCode();

    public static bool operator ==(Tick a, Tick b)
    {
        if (a.TickRate != b.TickRate)
            throw new InvalidOperationException("TickRate value doesn't match");
        return a.TickCount == b.TickCount && a.TickDuration == b.TickDuration;
    }

    public static bool operator !=(Tick a, Tick b) => !(a == b);

    public static bool operator <(Tick a, Tick b)
    {
        if (a.TickRate != b.TickRate)
            throw new InvalidOperationException("TickRate value doesn't match");
        return a.TickCount < b.TickCount || a.TickDuration < b.TickDuration;
    }

    public static bool operator >(Tick a, Tick b)
    {
        if (a.TickRate != b.TickRate)
            throw new InvalidOperationException("TickRate value doesn't match");
        return a.TickCount > b.TickCount || a.TickDuration > b.TickDuration;
    }

    public static bool operator <=(Tick a, Tick b) => !(a > b);

    public static bool operator >=(Tick a, Tick b) => !(a < b);

    public override readonly string ToString() =>
        $"{TickCount}:{TickDuration:0.####}";
}
