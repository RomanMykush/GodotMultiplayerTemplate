using Godot;
using GodotMultiplayerTemplate.Shared;
using System;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Client;

public partial class MainGameHost : MainGameClient
{
    public int MaxClients { get; private set; }

    public void SetupHost(int port, int maxClients)
    {
        Address = "localhost";
        Port = port;
        MaxClients = maxClients;
    }

    public async override Task<PreInitResult> PreInitialize()
    {
        var args = new string[]
        {
            "--headless",
            "++", // delimeter of user defined arguments
            "--hosted",
            "-p", $"{Port}",
            "-n", $"{MaxClients}"
        };

        int pid;
        if (OS.HasFeature("editor"))
        {
            args = [.. args, "--test-server"];
            // Run other instance of editor build as a server
            pid = OS.CreateInstance(args);
        }
        else
        {
            // Get executable file extension
            var fileExt = OS.GetName() switch
            {
                "Windows" => ".exe",
                // "MacOS" => ???,
                "Linux" => ".x86_64",
                // "FreeBSD" or "NetBSD" or "OpenBSD" or "BSD" => ".elf", - potentially can be added in future
                _ => throw new Exception("Unsupported hosting platform")
            };

            // Run server executable
            pid = OS.CreateProcess($"server/server{fileExt}", args);
        }
        // Check if process started
        if (pid == -1)
            return new PreInitResult(false, "Failed to start server");

        // PreInitialize client
        return await base.PreInitialize();
    }
}
