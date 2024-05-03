using Godot;
using MemoryPack;
using SteampunkDnD.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class Network : Node
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(GodotWrapper<IMessage> message);

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

        var semaphore = new SemaphoreSlim(0, 1);
        bool connectionStarted = false;
        // Request deffered start of connection
        Callable.From(() =>
        {
            ENetMultiplayerPeer peer = new();
            var clientStatus = peer.CreateClient(address, port);
            // Success if correct address format was supplied
            if (clientStatus == Error.Ok)
                Multiplayer.MultiplayerPeer = peer;
            // Return result
            connectionStarted = clientStatus == Error.Ok;
            semaphore.Release();
        }).CallDeferred();

        // Wait for connection to start
        await semaphore.WaitAsync();
        if (!connectionStarted)
            return false;

        // Wait for connection to finish
        int index = -1;
        var multiplayer = Multiplayer; // workaround error
        await Task.Run(() =>
        {
            index = Task.WaitAny(
                    Task.Run(async () => await multiplayer.ToSignal(multiplayer, MultiplayerApi.SignalName.ConnectedToServer)),
                    Task.Run(async () => await multiplayer.ToSignal(multiplayer, MultiplayerApi.SignalName.ConnectionFailed)));
        });

        // Log result
        if (index == 0)
            Logger.Singleton.Log(LogLevel.Info, $"Successfully established connection");
        else Logger.Singleton.Log(LogLevel.Info, $"Failed to establish connection");

        return index == 0;
    }

    public void Disconnect() =>
        Multiplayer.MultiplayerPeer = null;

    public void SendPacket(IMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable)
    {
        byte[] data = MemoryPackSerializer.Serialize(message);
        var transmitter = Multiplayer as SceneMultiplayer;
        // TODO: Add channels mapping to IMessage implementation type for faster Reliable and UnreliableOrdered transfer modes
        transmitter.SendBytes(data, 1, mode, 0);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var message = MemoryPackSerializer.Deserialize<IMessage>(data);
        EmitSignal(SignalName.MessageReceived, new GodotWrapper<IMessage>(message));
    }
}
