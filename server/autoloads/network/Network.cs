using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Server;

public partial class Network : NetworkBase
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(int peer, GodotWrapper<INetworkMessage> wrapper);
    public const int BroadcastPeer = 0;

    public override void _Ready()
    {
        Singleton = this;
        base._Ready();
    }

    public void StartServer(int port, int maxPlayers)
    {
        // Start ENet server
        ENetMultiplayerPeer peer = new();
        var status = peer.CreateServer(port, maxPlayers);
        if (status != Error.Ok)
            throw new Exception("Failed to start server.");

        Multiplayer.MultiplayerPeer = peer;
    }

    protected override void OnMessageReceived(int peer, INetworkMessage message)
    {
        var wrapper = new GodotWrapper<INetworkMessage>(message);
        EmitSignal(SignalName.MessageReceived, peer, wrapper);
    }
}
