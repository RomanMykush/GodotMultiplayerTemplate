using Godot;
using MemoryPack;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public partial class Network : Node
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(int peer, GodotWrapper<IMessage> message);

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

    public void SendPacket(int peer, IMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        // TODO: Add channels mapping to IMessage implementation type for Reliable and UnreliableOrdered transfer modes
        transmitter.SendBytes(data, peer, mode, 0);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var message = MemoryPackSerializer.Deserialize<IMessage>(data);
        EmitSignal(SignalName.MessageReceived, (int)id, new GodotWrapper<IMessage>(message));
    }
}
