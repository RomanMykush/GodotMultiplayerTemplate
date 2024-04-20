using Godot;
using SteampunkDnD.Shared;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class MainGameHost : MainGameClient
{
    public int MaxClients { get; private set; }

    protected MainGameHost() { }

    public static MainGameHost CreateHost(int port, int maxClients)
    {
        return new MainGameHost()
        {
            Address = "localhost",
            Port = port,
            MaxClients = maxClients
        };
    }

    public override Task<InitLevelResult> Initialize()
    {
        // TODO: Implement host initialization
        // Initialize client
        return base.Initialize();
    }
}
