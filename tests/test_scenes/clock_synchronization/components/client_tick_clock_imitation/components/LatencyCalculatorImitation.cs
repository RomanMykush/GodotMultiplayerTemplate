using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Tests;

public partial class LatencyCalculatorImitation : Node
{
    [Signal] public delegate void LatencyCalculatedEventHandler(float avarage, float std);

    [Export] private uint MaxSampleSize = 30;

    private readonly Queue<float> LatencySamples = new(); // Latency samples of sync message in seconds

    public void OnSyncReceived(Sync sync)
    {
        AppendLatency(sync);
        CalculateLatency();
    }

    private void AppendLatency(Sync sync)
    {
        uint currentTime = (uint)Time.GetTicksMsec();

        // Update samples
        float avarageLatency = (currentTime - sync.ClientTime) / 2f; // in milliseconds
        LatencySamples.Enqueue(avarageLatency / 1000);
        if (LatencySamples.Count > MaxSampleSize)
            LatencySamples.Dequeue();
    }

    private void CalculateLatency()
    {
        if (LatencySamples.Count < 2)
            return;

        // Calculate numerical characteristics of distribution
        float avarage = LatencySamples.Sum(x => x) / LatencySamples.Count;
        float dispersion = LatencySamples.Sum(x => { float res = x - avarage; return res * res; }) / (LatencySamples.Count - 1);
        float std = Mathf.Sqrt(dispersion);

        EmitSignal(SignalName.LatencyCalculated, avarage, std);
    }

    public void ClearState()
    {
        LatencySamples.Clear();
    }
}
