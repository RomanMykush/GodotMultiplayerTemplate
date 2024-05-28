using Godot;
using SteampunkDnD.Shared;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Server;

public partial class Synchronizer : Node
{
    [Signal] public delegate void TickUpdatedEventHandler(int currentTick);

    public static Synchronizer Singleton { get; private set; }
    public uint CurrentTick { get; private set; }

    public override void _Ready()
    {
        Singleton = this;
        // Subscribe to events
        Network.Singleton.MessageReceived += (peer, msg) =>
        {
            if (msg.Value is Sync sync)
                OnSyncReceived(peer, sync);
        };
        Multiplayer.PeerConnected += OnPeerConnected;
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentTick++;
        EmitSignal(SignalName.TickUpdated, CurrentTick);
    }

    private void OnSyncReceived(int peer, Sync sync)
    {
        var reply = sync with { ServerTick = CurrentTick };
        Network.Singleton.SendPacket(peer, reply);
    }

    private async void OnPeerConnected(long peer)
    {
        await Task.Delay(500); // TODO: Replace with a better solution for client initialization

        var tickRate = Engine.PhysicsTicksPerSecond;
        var syncInfo = new SyncInfo(tickRate);
        Network.Singleton.SendPacket((int)peer, syncInfo);
    }
}
