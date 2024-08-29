using Godot;
using System;
using System.Threading;

namespace SteampunkDnD.Shared;

public partial class AppManager : Node
{
    public static AppManager Singleton { get; private set; }

    // Signals
    [Signal] public delegate void ExitingEventHandler();

    // Exports
    [Export] public int DefaultPort { get; private set; }
    [Export] public int DefaultMaxPlayers { get; private set; }

    public static readonly SynchronizationContext MainThreadSyncContext = SynchronizationContext.Current;

    public override void _Ready() =>
        Singleton = this;

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            Exit();
    }

    public void Exit()
    {
        Logger.Singleton.Log(LogLevel.Trace, "App shutdown");

        EmitSignal(SignalName.Exiting);

        if (GetTree().CurrentScene is IGameMode level)
            level.CleanUp();

        GetTree().Quit();
    }
}
