using Godot;
using SteampunkDnD.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

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
#if TOOLS
        args = args.Append("--test-server").ToArray();
        // Run other instance of editor build as a server
        var pid = OS.CreateInstance(args);

        // Check if process started
        if (pid == -1)
            return new PreInitResult(false, "Failed to start server");
#else
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
        var pid = OS.CreateProcess($"server/server{fileExt}", args);

        // Check if process started
        if (pid == -1)
            return new PreInitResult(false, "Failed to start server");
#endif
        // PreInitialize client
        return await base.PreInitialize();
    }
}
