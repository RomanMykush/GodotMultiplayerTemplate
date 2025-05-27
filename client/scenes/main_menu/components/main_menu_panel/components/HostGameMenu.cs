using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Client;

public partial class HostGameMenu : MarginContainer
{
    private void HostGame()
    {
        // TODO: Add more configurations
        // Get configurations
        var port = AppManager.Singleton.DefaultPort;
        var maxPlayers = AppManager.Singleton.DefaultMaxPlayers;
        // Create host
        var node = SceneFactory.Singleton.CreateMainHost(port, maxPlayers);
        SceneTransitioner.Singleton.TryChangeScene(node);
    }
}
