using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Client;

public partial class ClientStartUp : PlatformStartUp
{
    public override void AfterReady()
    {
        Node node;
        // Skip intro if editor build
        if (!OS.HasFeature("editor"))
            node = SceneFactory.Singleton.CreateMainMenu();
        else node = SceneFactory.Singleton.CreateIntro();
        SceneTransitioner.Singleton.TryChangeScene(node, true);

        Logger.Singleton.Log(LogLevel.Trace, "Client started");
    }
}
