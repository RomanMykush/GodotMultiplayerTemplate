using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class ServerStartUp : PlatformStartUp
{
    public override void AfterReady()
    {
        Logger.Singleton.Log(LogLevel.Trace, "Server started");
    }
}
