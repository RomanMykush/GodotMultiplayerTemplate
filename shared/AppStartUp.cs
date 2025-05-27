using Godot;
using System;
using System.Linq;

namespace GodotMultiplayerTemplate.Shared;

public partial class AppStartUp : Node
{
    [Export(PropertyHint.File, "*.tscn")] private string ClientStartUpPath { get; set; }
    [Export(PropertyHint.File, "*.tscn")] private string ServerStartUpPath { get; set; }
    public override void _Ready()
    {
        Logger.Singleton.Log(LogLevel.Trace, "App started");

        // Editor build
        if (OS.HasFeature("editor"))
        {
            if (OS.GetCmdlineUserArgs().Contains("--test-server"))
            {
                // Start as dedicated server
                StartUp(ServerStartUpPath);
                return;
            }
            // Start as client
            StartUp(ClientStartUpPath);
            return;
        }

        // Export build
        if (OS.HasFeature("dedicated_server"))
        {
            // Start as dedicated server
            StartUp(ServerStartUpPath);
            return;
        }
        // Start as client
        StartUp(ClientStartUpPath);
    }

    private void StartUp(string path) =>
        DeferredUtils.CallDeferred(() => GetTree().ChangeSceneToFile(path));
}
