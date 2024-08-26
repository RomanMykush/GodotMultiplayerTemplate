using Godot;
using System;

namespace SteampunkDnD.Client;

public partial class SceneFactory : Node
{
    public static SceneFactory Singleton { get; private set; }
    [Export] private PackedScene IntroScene;
    [Export] private PackedScene MainMenuScene;
    [Export] private PackedScene MainGameHostScene;
    [Export] private PackedScene MainGameClientScene;

    public override void _Ready() =>
        Singleton = this;

    public Node CreateIntro() =>
        IntroScene.Instantiate();

    public Node CreateMainMenu() =>
        MainMenuScene.Instantiate();

    public Node CreateMainHost(int port, int maxClients)
    {
        var host = MainGameHostScene.Instantiate() as MainGameHost;
        host.SetupHost(port, maxClients);
        return host;
    }

    public Node CreateMainClient(string ip, int port)
    {
        var client = MainGameClientScene.Instantiate() as MainGameClient;
        client.SetupClient(ip, port);
        return client;
    }
}
