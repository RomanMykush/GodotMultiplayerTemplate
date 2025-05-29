using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Tests;

public partial class ClientTickClockImitation : Node
{
    [Signal] public delegate void SendingSyncRequestEventHandler(GodotWrapper<SyncRequest> wrapper);

    private LatencyCalculatorImitation Calculator;
    private TickSynchronizerImitation Synchronizer;

    public uint CurrentTick => Synchronizer.CurrentTick;
    public float PreferredTick => Synchronizer.PreferredTick;

    public override void _Ready()
    {
        Calculator = GetNode<LatencyCalculatorImitation>("%Calculator");
        Synchronizer = GetNode<TickSynchronizerImitation>("%Synchronizer");

        var syncPinger = GetNode<Timer>("%Pinger");
        syncPinger.Timeout += () =>
        {
            var request = new SyncRequest((uint)Time.GetTicksMsec());
            var wrapper = new GodotWrapper<SyncRequest>(request);
            EmitSignal(SignalName.SendingSyncRequest, wrapper);
        };
        syncPinger.Start();
    }

    public void Update(double delta)
    {
        Synchronizer.UpdateTick((float)delta);
    }

    public void OnSyncReceived(Sync sync)
    {
        Calculator.OnSyncReceived(sync);
        Synchronizer.OnSyncReceived(sync);
    }

    public void ClearState()
    {
        Calculator.ClearState();
        Synchronizer.ClearState();
    }
}
