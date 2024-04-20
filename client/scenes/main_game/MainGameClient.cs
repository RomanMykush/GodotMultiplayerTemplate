using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class MainGameClient : Node, ILevel
{
    public string Address { get; protected set; }
    public int Port { get; protected set; }

    protected MainGameClient() { }

    public static MainGameClient CreateClient(string address, int port)
    {
        return new MainGameClient()
        {
            Address = address,
            Port = port
        };
    }

    public virtual async Task<InitLevelResult> Initialize()
    {
        // TODO: Implement client initialization
        throw new NotImplementedException();
    }

    public IEnumerable<JobObserver> StartConstruction()
    {
        // TODO: Implement level construction
        throw new NotImplementedException();
    }

    public void CleanUp()
    {
        // TODO: Implement clean up
        throw new NotImplementedException();
    }
}
