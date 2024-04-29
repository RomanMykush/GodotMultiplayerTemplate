using Godot;
using System;

namespace SteampunkDnD.Shared;

public partial class Logger : Node
{
    public static Logger Singleton { get; private set; }

    public override void _Ready() =>
        Singleton = this;

    public void Log(LogLevel level, string message)
    {
        // Get prefix and color
        var levelPrefix = level switch
        {
            LogLevel.Fatal => "[color=darkred]FATAL",
            LogLevel.Error => "[color=red]ERROR",
            LogLevel.Warning => "[color=gold]WARN",
            LogLevel.Info => "[color=skyblue]INFO",
            LogLevel.Debug => "[color=purple]DEBUG",
            LogLevel.Trace => "[color=gray]TRACE",
            _ => throw new NotImplementedException("This value of LogLevel was not implemented")
        };
        // Error print out
        switch (level)
        {
            case LogLevel.Fatal:
            case LogLevel.Error:
                GD.PushError(message);
                break;
            case LogLevel.Warning:
                GD.PushWarning(message);
                break;
        }
        // Log print out
        GD.PrintRich($"{levelPrefix}: {message}[/color]");
    }
}

public enum LogLevel
{
    Fatal,
    Error,
    Warning,
    Info,
    Debug,
    Trace
}