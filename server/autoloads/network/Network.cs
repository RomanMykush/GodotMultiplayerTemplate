using Godot;
using MemoryPack;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class Network : Node
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(int peer, GodotWrapper<INetworkMessage> wrapper);

    public override void _Ready()
    {
        Singleton = this;
        // Subsribe to messages
        if (Multiplayer is SceneMultiplayer sceneMultiplayer)
            sceneMultiplayer.PeerPacket += OnPacketReceived;
        else Logger.Singleton.Log(LogLevel.Error, "Property Multiplayer does not contain an instance of SceneMultiplayer");
    }

    public void StartServer(int port, int maxPlayers)
    {
        // Start ENet server
        ENetMultiplayerPeer peer = new();
        peer.CreateServer(port, maxPlayers);
        Multiplayer.MultiplayerPeer = peer;
    }

    public void SendPacket(int peer, INetworkMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        // TODO: Add channels mapping to INetworkMessage implementation type for Reliable and UnreliableOrdered transfer modes
        transmitter.SendBytes(data, peer, mode, 0);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        try
        {
            var message = MemoryPackSerializer.Deserialize<INetworkMessage>(data);
            EmitSignal(SignalName.MessageReceived, (int)id, new GodotWrapper<INetworkMessage>(message));
        }
        catch (MemoryPackSerializationException)
        {
            Logger.Singleton.Log(LogLevel.Error, "Invalid data has been received");
        }
    }
}
