using Godot;
using MemoryPack;
using SteampunkDnD.Shared;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class Network : Node
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(GodotWrapper<INetworkMessage> wrapper);

    public override void _Ready()
    {
        Singleton = this;
        // Subsribe to events
        Multiplayer.ConnectionFailed += Disconnect;
        if (Multiplayer is SceneMultiplayer sceneMultiplayer)
            sceneMultiplayer.PeerPacket += OnPacketReceived;
        else Logger.Singleton.Log(LogLevel.Error, "Property Multiplayer does not contain an instance of SceneMultiplayer");
    }

    /// <returns>true if trying to establish connect or already connected to server; otherwise false.</returns>
    private bool IsBusy() => Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer
            && Multiplayer.MultiplayerPeer != null;

    public async Task<bool> Connect(string address, int port)
    {
        Logger.Singleton.Log(LogLevel.Info, $"Trying to connect to server");
        if (IsBusy())
        {
            Logger.Singleton.Log(LogLevel.Warning, "Connecting to server while connected/connecting to other");
            Disconnect();
        }

        bool connectionStarted = await DeferredUtils.RunDeferred(() =>
        {
            ENetMultiplayerPeer peer = new();
            var clientStatus = peer.CreateClient(address, port);
            // Success if correct address format was supplied
            if (clientStatus == Error.Ok)
                Multiplayer.MultiplayerPeer = peer;
            // Return result
            return clientStatus == Error.Ok;
        });

        if (!connectionStarted)
            return false;

        // Wait for connection to finish
        var successAwaiter = await DeferredUtils.RunDeferred(() => ToSignal(Multiplayer, MultiplayerApi.SignalName.ConnectedToServer));
        var failAwaiter = await DeferredUtils.RunDeferred(() => ToSignal(Multiplayer, MultiplayerApi.SignalName.ConnectionFailed));
        var successTask = Task.Run(async () => await successAwaiter);
        var failTask = Task.Run(async () => await failAwaiter);

        await Task.WhenAny(successTask, failTask);

        // Log result
        if (successTask.IsCompletedSuccessfully)
        {
            Logger.Singleton.Log(LogLevel.Info, $"Successfully established connection");
            return true;
        }
        Logger.Singleton.Log(LogLevel.Info, $"Failed to establish connection");

        return false;
    }

    public void Disconnect() =>
        Multiplayer.MultiplayerPeer = null;

    public void SendPacket(INetworkMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        // TODO: Add channels mapping to INetworkMessage implementation type for faster Reliable and UnreliableOrdered transfer modes
        transmitter.SendBytes(data, 1, mode, 0);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var message = MemoryPackSerializer.Deserialize<INetworkMessage>(data);
        EmitSignal(SignalName.MessageReceived, new GodotWrapper<INetworkMessage>(message));
    }
}
