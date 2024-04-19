using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Client;

public partial class ClientStartUp : PlatformStartUp
{
    public override void AfterReady()
    {
        GD.Print("Client started");
    }
}
