using Godot;
using MemoryPack;

namespace SteampunkDnD.Shared;

public abstract partial class NetworkBase : Node
{
    public override void _Ready()
    {
        // Subsribe to events
        if (Multiplayer is SceneMultiplayer sceneMultiplayer)
            sceneMultiplayer.PeerPacket += OnPacketReceived;
        else Logger.Singleton.Log(LogLevel.Error, "Property Multiplayer does not contain an instance of SceneMultiplayer");
    }

    public void SendMessage(int peer, INetworkMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        // TODO: Add channels mapping to INetworkMessage implementation type for faster Reliable and UnreliableOrdered transfer modes
        transmitter.SendBytes(data, peer, mode, 0);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        try
        {
            var message = MemoryPackSerializer.Deserialize<INetworkMessage>(data);
            OnMessageReceived((int)id, message);
        }
        catch (MemoryPackSerializationException)
        {
            Logger.Singleton.Log(LogLevel.Warning, "Invalid data has been received");
        }
    }

    protected abstract void OnMessageReceived(int peer, INetworkMessage message);
}
