using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class Network : Node
{
    public static Network Singleton { get; private set; }
    [Export] public int DefaultPort { get; set; }
    [Export] public int DefaultMaxPlayers { get; set; }

    public override void _Ready()
    {
        Singleton = this;
        // Get port
        int port = DefaultPort;
        if (CmdUtils.GetParameterValue("-p", out int value))
            port = value;

        // Get max players
        int maxPlayers = DefaultMaxPlayers;
        if (CmdUtils.GetParameterValue("-n", out value))
            maxPlayers = value;

        // Start ENet server
        ENetMultiplayerPeer peer = new();
        peer.CreateServer(port, maxPlayers);
        Multiplayer.MultiplayerPeer = peer;
    }
}
