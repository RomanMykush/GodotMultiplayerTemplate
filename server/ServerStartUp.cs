using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class ServerStartUp : PlatformStartUp
{
    public override void AfterReady()
    {
        // Get port
        int port = AppManager.Singleton.DefaultPort;
        if (CmdUtils.GetParameterValue("-p", out int value))
            port = value;

        // Get max players
        int maxPlayers = AppManager.Singleton.DefaultMaxPlayers;
        if (CmdUtils.GetParameterValue("-n", out value))
            maxPlayers = value;
        
        Network.Singleton.StartServer(port, maxPlayers);
        Logger.Singleton.Log(LogLevel.Trace, "Server started");
    }
}
