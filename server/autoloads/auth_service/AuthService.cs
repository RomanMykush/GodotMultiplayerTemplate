using Godot;
using GodotMultiplayerTemplate.Shared;
using System.Collections.Generic;
using MemoryPack;

namespace GodotMultiplayerTemplate.Server;

// WARN: This service does NOT use secure authentication
public partial class AuthService : Node
{
    public static AuthService Singleton { get; private set; }

    [Signal] public delegate void PlayerJoinedEventHandler(uint playerId);

    private uint ServerId;
    private List<uint> KnownPlayers;
    private readonly BidirectionalDictionary<int, uint> ClientToPlayerMap = new();

    public override void _Ready()
    {
        Singleton = this;

        var transmitter = Multiplayer as SceneMultiplayer;

        transmitter.AuthCallback = new Callable(this, MethodName.OnAuthReceived);
        transmitter.PeerAuthenticating += OnPeerAuthenticating;
        transmitter.PeerDisconnected += OnPeerDisconnected;

        // TODO: Add ServerId serialization to and deserialization from save data
        ServerId = GenerateId();
        KnownPlayers = new();
    }

    public uint? GetPlayerId(int peer)
    {
        if (!ClientToPlayerMap.Forward.Contains(peer))
            return null;
        return ClientToPlayerMap.Forward[peer];
    }

    private void SendMessage(int peer, INetworkMessage message)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        transmitter.SendAuth(peer, data);
    }

    private static uint GenerateId() =>
        (uint)((long)(Time.GetUnixTimeFromSystem() * 1000) % uint.MaxValue);

    private void CompleteAuth(int peer, uint playerId)
    {
        var transmitter = Multiplayer as SceneMultiplayer;
        ClientToPlayerMap.Add(peer, playerId);
        transmitter.CompleteAuth(peer);
        EmitSignal(SignalName.PlayerJoined, playerId);
    }

    private void OnMessageReceived(int peer, INetworkMessage message)
    {
        switch (message)
        {
            case ClientAuth clientAuth:
                if (ClientToPlayerMap.Reverse.Contains(clientAuth.PlayerId))
                {
                    Logger.Singleton.Log(LogLevel.Warning, $"Client {peer} tried to authenticate, but another client with same PlayerId already authenticated");
                    return;
                }

                if (!KnownPlayers.Contains(clientAuth.PlayerId))
                {
                    Logger.Singleton.Log(LogLevel.Warning, $"Client {peer} passed unknown PlayerId");
                    return;
                }

                CompleteAuth(peer, clientAuth.PlayerId);
                break;
            case NewPlayerIdRequest:
                var playerId = GenerateId();
                // This is in case another client authenticate at the same millisecond
                while (KnownPlayers.Contains(playerId))
                    playerId++;
                KnownPlayers.Add(playerId);

                var newPlayerId = new NewPlayerId(playerId);
                SendMessage(peer, newPlayerId);

                CompleteAuth(peer, playerId);
                break;
            default:
                Logger.Singleton.Log(LogLevel.Warning, $"Client {peer} sent incorrect auth message to server");
                break;
        }
    }

    private void OnAuthReceived(int peer, byte[] data)
    {
        try
        {
            var message = MemoryPackSerializer.Deserialize<INetworkMessage>(data);
            OnMessageReceived(peer, message);
        }
        catch (MemoryPackSerializationException)
        {
            Logger.Singleton.Log(LogLevel.Error, "Invalid data has been received during authentication");
        }
    }

    private void OnPeerAuthenticating(long id)
    {
        var serverAuth = new ServerAuth(ServerId);
        SendMessage((int)id, serverAuth);
    }

    private void OnPeerDisconnected(long id) =>
        ClientToPlayerMap.RemoveByFirstKey((int)id);
}
