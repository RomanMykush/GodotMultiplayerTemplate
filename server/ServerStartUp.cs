using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class ServerStartUp : PlatformStartUp
{
    public override void AfterReady()
    {
        GD.Print("Server started");
    }
}
