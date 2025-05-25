using System.Diagnostics;
using Godot;
using SteampunkDnD.Shared;

namespace SteampunkDnD.Server;

public partial class TickClock : Node
{
    public static TickClock Singleton { get; private set; }
    [Signal] public delegate void TickUpdatedEventHandler(uint currentTick);
    public uint CurrentTick { get; private set; }
    private readonly Stopwatch CurrentTickWatch = new();

    public override void _Ready()
    {
        Singleton = this;
        // Subscribe to events
        Network.Singleton.MessageReceived += (peer, wrapper) =>
        {
            switch (wrapper.Value)
            {
                case SyncRequest sync:
                    OnSyncReceived(peer, sync);
                    break;
                case SyncInfoRequest decimalValue:
                    OnSyncInfoReceived(peer);
                    break;
            }
        };
    }

    public override void _PhysicsProcess(double _)
    {
        CurrentTick++;
        CurrentTickWatch.Restart();
        EmitSignal(SignalName.TickUpdated, CurrentTick);
    }

    private void OnSyncReceived(int peer, SyncRequest sync)
    {
        var reply = new Sync(sync.ClientTime, CurrentTick, (float)CurrentTickWatch.Elapsed.TotalSeconds);
        Network.Singleton.SendMessage(peer, reply);
    }

    private void OnSyncInfoReceived(int peer)
    {
        var tickRate = Engine.PhysicsTicksPerSecond;
        var syncInfo = new SyncInfo(tickRate);
        Network.Singleton.SendMessage(peer, syncInfo);
    }
}
