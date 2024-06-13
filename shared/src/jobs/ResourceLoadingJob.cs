using Godot;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public partial class ResourceLoadingJob : Job
{
    private Timer UpdateTimer = new();
    public readonly string ResourcePath;

    public ResourceLoadingJob(string resourcePath)
    {
        ResourcePath = resourcePath;

        // Setup timer
        UpdateTimer.WaitTime = 0.1;
        UpdateTimer.OneShot = false;
        AddChild(UpdateTimer);
    }

    public override async Task Run()
    {
        if (string.IsNullOrWhiteSpace(ResourcePath))
        {
            Logger.Singleton.Log(LogLevel.Error, "ResourcePath was not set");
            DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
            throw new ArgumentException("ResourcePath was not set");
        }

        // Start loading
        ResourceLoader.LoadThreadedRequest(ResourcePath);
        UpdateTimer.Start();

        while (true)
        {
            Godot.Collections.Array progressArray = new();
            var status = ResourceLoader.LoadThreadedGetStatus(ResourcePath, progressArray);

            // Report resource loading status 
            switch (status)
            {
                case ResourceLoader.ThreadLoadStatus.InProgress:
                    var metric = new JobMetric((float)progressArray[0], "Loading...");
                    DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Updated, new GodotWrapper<JobMetric>(metric)));
                    break;
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Completed));
                    return;
                case ResourceLoader.ThreadLoadStatus.Failed:
                    Logger.Singleton.Log(LogLevel.Error, "Failed to load resource");
                    DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
                    throw new ResourceLoadingException("Failed to load resource");
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                    Logger.Singleton.Log(LogLevel.Error, "The resource is invalid");
                    DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
                    throw new ArgumentException("The resource is invalid");
            }

            await ToSignal(UpdateTimer, Timer.SignalName.Timeout);
        }
    }
}
