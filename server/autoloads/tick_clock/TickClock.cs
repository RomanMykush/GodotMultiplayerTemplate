using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class TickClock : Node
{
    public static TickClock Singleton { get; private set; }
    [Signal] public delegate void TickUpdatedEventHandler(uint currentTick, float tickTimeDelta);
    public uint CurrentTick { get; private set; }

    public override void _Ready()
    {
        Singleton = this;
        // Subscribe to events
        Network.Singleton.MessageReceived += (peer, wrapper) =>
        {
            switch (wrapper.Value) {
            case Sync sync:
                OnSyncReceived(peer, sync);
                break;
            case SyncInfoRequest decimalValue:
                OnSyncInfoRequestReceived(peer);
                break;
            }
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentTick++;
        EmitSignal(SignalName.TickUpdated, CurrentTick, (float)delta);
    }

    private void OnSyncReceived(int peer, Sync sync)
    {
        var reply = sync with { ServerTick = CurrentTick };
        Network.Singleton.SendPacket(peer, reply);
    }

    private void OnSyncInfoRequestReceived(int peer)
    {
        var tickRate = Engine.PhysicsTicksPerSecond;
        var syncInfo = new SyncInfo(tickRate);
        Network.Singleton.SendPacket(peer, syncInfo);
    }
}
