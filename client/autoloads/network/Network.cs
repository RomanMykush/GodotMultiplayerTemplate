using Godot;
using SteampunkDnD.Shared;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class Network : NetworkBase
{
    public static Network Singleton { get; private set; }
    [Signal] public delegate void MessageReceivedEventHandler(GodotWrapper<INetworkMessage> wrapper);
    public const int ServerPeer = 1;

    public override void _Ready()
    {
        Singleton = this;

        Multiplayer.ConnectionFailed += Disconnect;
        base._Ready();
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
            var status = peer.CreateClient(address, port);
            // Success if correct address format was supplied
            if (status == Error.Ok)
                Multiplayer.MultiplayerPeer = peer;
            // Return result
            return status == Error.Ok;
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

    public void SendMessage(INetworkMessage message, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Unreliable) =>
        SendMessage(ServerPeer, message, mode);

    public void Disconnect() =>
        Multiplayer.MultiplayerPeer = null;

    protected override void OnMessageReceived(int peer, INetworkMessage message)
    {
        if (peer != ServerPeer)
        {
            Logger.Singleton.Log(LogLevel.Warning, $"Received message directly from non-server peer {peer}. Ignoring it");
            return;
        }
        var wrapper = new GodotWrapper<INetworkMessage>(message);
        EmitSignal(SignalName.MessageReceived, wrapper);
    }
}
