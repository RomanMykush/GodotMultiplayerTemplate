using Godot;
using System;
using System.Linq;

namespace SteampunkDnD.Shared;

public partial class AppStartUp : Node
{
    [Export] private string ClientStartUpPath { get; set; }
    [Export] private string ServerStartUpPath { get; set; }
    public override void _Ready()
    {
#if TOOLS           // Editor build
        if (OS.GetCmdlineUserArgs().Contains("--test-server"))
        {
            StartUp(ServerStartUpPath);
            return;
        }
        StartUp(ClientStartUpPath);
#elif GODOT_SERVER  // Start as dedicated server
        StartUp(ServerStartUpPath);
#else               // Start as client
        StartUp(ClientStartUpPath);
#endif
        GD.Print("App started");
    }

    private void StartUp(string path) =>
        Callable.From(() => GetTree().ChangeSceneToFile(path)).CallDeferred();
}