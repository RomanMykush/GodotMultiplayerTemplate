using Godot;
using System;

namespace SteampunkDnD.Shared;

public partial class LoadingObserver : JobObserver
{
    private Timer UpdateTimer = new();
    public string ResourcePath;

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(ResourcePath))
        {
            Logger.Singleton.Log(LogLevel.Error, "ResourcePath was not set");
            return;
        }

        // Setup timer
        UpdateTimer.WaitTime = 0.1;
        UpdateTimer.OneShot = false;
        AddChild(UpdateTimer);

        UpdateTimer.Timeout += () =>
        {
            Godot.Collections.Array progressArray = new();
            var status = ResourceLoader.LoadThreadedGetStatus(ResourcePath, progressArray);

            // Report resource loading status 
            switch (status)
            {
                case ResourceLoader.ThreadLoadStatus.InProgress:
                    var metric = new JobMetric((float)progressArray[0], "Loading...");
                    EmitSignal(SignalName.Updated, new GodotWrapper<JobMetric>(metric));
                    break;
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    EmitSignal(SignalName.Completed);
                    break;
                case ResourceLoader.ThreadLoadStatus.Failed:
                    Logger.Singleton.Log(LogLevel.Error, "Failed to load resource");
                    EmitSignal(SignalName.Failed);
                    break;
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                    Logger.Singleton.Log(LogLevel.Error, "Invalid ResourcePath specified");
                    EmitSignal(SignalName.Failed);
                    break;
            }
        };

        // Start observation
        UpdateTimer.Start();
    }
}
