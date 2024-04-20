using Godot;
using System;

namespace SteampunkDnD.Shared;

public partial class AppManager : Node
{
    public static AppManager Singleton { get; private set; }

    // Signals
    [Signal] public delegate void ExitingEventHandler();

    // Exports
    [Export] public int DefaultPort { get; private set; }
    [Export] public int DefaultMaxPlayers { get; private set; }

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

        if (GetTree().CurrentScene is ILevel level)
            level.CleanUp();

        GetTree().Quit();
    }
}
