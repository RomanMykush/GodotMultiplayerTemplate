using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Server;

public partial class HostChecker : Node
{
    public long? HostId { get; private set; }
    public override void _Ready()
    {
        // Check if hosted from client
        if (!CmdUtils.CheckFlag("--hosted"))
            return;

        Multiplayer.PeerConnected += CheckHostConnected;
        Multiplayer.PeerDisconnected += CheckHostDisconnected;
    }

    private void CheckHostConnected(long id) =>
        HostId ??= id;

    private void CheckHostDisconnected(long id)
    {
        if (HostId == id)
            AppManager.Singleton.Exit();
    }
}
