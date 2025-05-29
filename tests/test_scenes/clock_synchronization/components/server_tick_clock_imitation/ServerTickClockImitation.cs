using Godot;
using GodotMultiplayerTemplate.Shared;
using System.Diagnostics;

namespace GodotMultiplayerTemplate.Tests;

public partial class ServerTickClockImitation : Node
{
    [Signal] public delegate void SendingSyncEventHandler(GodotWrapper<Sync> wrapper);

    public uint CurrentTick { get; private set; }
    private readonly Stopwatch CurrentTickWatch = new();

    public void Update()
    {
        CurrentTick++;
        CurrentTickWatch.Restart();
    }

    public void OnSyncReceived(SyncRequest sync)
    {
        var reply = new Sync(sync.ClientTime, CurrentTick, (float)CurrentTickWatch.Elapsed.TotalSeconds);
        var wrapper = new GodotWrapper<Sync>(reply);
        EmitSignal(SignalName.SendingSync, wrapper);
    }

    public void ClearState()
    {
        CurrentTick = 0;
        CurrentTickWatch.Reset();
    }
}
