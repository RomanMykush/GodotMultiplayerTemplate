using Godot;
using System;

namespace SteampunkDnD.Client;

public partial class SceneFactory : Node
{
    public static SceneFactory Singleton { get; private set; }
    [Export] private PackedScene _intro;
    [Export] private PackedScene _mainMenu;
    [Export] private PackedScene _mainGameHost;
    [Export] private PackedScene _mainGameClient;

    public override void _Ready() =>
        Singleton = this;

    public Node CreateIntro() =>
        _intro.Instantiate();

    public Node CreateMainMenu() =>
        _mainMenu.Instantiate();

    public Node CreateMainHost(int port, int maxClients)
    {
        var host = _mainGameHost.Instantiate() as MainGameHost;
        host.SetupHost(port, maxClients);
        return host;
    }


    public Node CreateMainClient(string ip, int port)
    {
        var client = _mainGameClient.Instantiate() as MainGameClient;
        client.SetupClient(ip, port);
        return client;
    }
}
