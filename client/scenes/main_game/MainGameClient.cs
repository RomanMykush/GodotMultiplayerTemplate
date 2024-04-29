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

    public void SetupClient(string address, int port)
    {
        Address = address;
        Port = port;
    }

    public virtual async Task<InitLevelResult> Initialize()
    {
        var result = await Network.Singleton.Connect(Address, Port);
        return new InitLevelResult(result, "Failed to connect to server");
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
