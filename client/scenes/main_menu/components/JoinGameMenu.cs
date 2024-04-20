using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Client;

public partial class JoinGameMenu : MarginContainer
{
    private LineEdit IpEdit;
    public override void _Ready() =>
        IpEdit = GetNode<LineEdit>("%IpEdit");

    private void JoinGame()
    {
        // TODO: Add more configurations
        // Get configurations
        var port = AppManager.Singleton.DefaultPort;
        // Create client
        var node = SceneFactory.Singleton.CreateMainClient(IpEdit.Text, port);
        SceneTransitioner.Singleton.TryChangeScene(node);
    }
}
