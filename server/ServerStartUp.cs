using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Server;

public partial class ServerStartUp : PlatformStartUp
{
    [Export] private PackedScene MainGame;

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
        GetTree().ChangeSceneToPacked(MainGame);
        Logger.Singleton.Log(LogLevel.Trace, "Server started");
    }
}
