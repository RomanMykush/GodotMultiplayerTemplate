using Godot;
using SteampunkDnD.Shared;
using System;
using MemoryPack;

namespace SteampunkDnD.Client;

// WARN: This service does NOT use secure authentication
public partial class AuthService : Node
{
    public static AuthService Singleton { get; private set; }

    public uint PlayerId { get; private set; }
    private const int ServerPeer = 1;

    public override void _Ready()
    {
        Singleton = this;

        var transmitter = Multiplayer as SceneMultiplayer;
        transmitter.AuthCallback = new Callable(this, MethodName.OnAuthReceived);
    }

    private void SendMessage(INetworkMessage message)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        transmitter.SendAuth(ServerPeer, data);
    }

    private void OnMessageReceived(INetworkMessage message)
    {
        var transmitter = Multiplayer as SceneMultiplayer;
        switch (message)
        {
            case ServerAuth serverAuth:
                // TODO: Check first for PlayerId from server info file and send ClientAuth instead if found
                var newMessage = new NewPlayerIdRequest();
                SendMessage(newMessage);
                break;
            case NewPlayerId newPlayerId:
                PlayerId = newPlayerId.PlayerId;
                // TODO: Save ServerId with corresponding PlayerId to files
                Logger.Singleton.Log(LogLevel.Info, $"Auth completed successfully");
                transmitter.CompleteAuth(ServerPeer);
                break;
            default:
                Logger.Singleton.Log(LogLevel.Warning, "Server sent incorrect auth message to server");
                break;
        }
    }

    private void OnAuthReceived(int _, byte[] data)
    {
        try
        {
            var message = MemoryPackSerializer.Deserialize<INetworkMessage>(data);
            OnMessageReceived(message);
        }
        catch (MemoryPackSerializationException)
        {
            Logger.Singleton.Log(LogLevel.Error, "Invalid data has been received during authentication");
        }
    }
}
