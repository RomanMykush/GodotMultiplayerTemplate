using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Client;

public partial class LevelLoadingBar : HBoxContainer
{
    // Child nodes
    private ProgressBar LoadingBar;
    private Label Description;

    // Other properties
    private JobObserver _observer;
    public JobObserver Observer
    {
        get => _observer;
        set
        {
            // Unsubscribe from previous observer
            if (_observer != null)
            {
                Logger.Singleton.Log(LogLevel.Warning, $"An instance of {nameof(LevelLoadingBar)} was already subscribed to other {nameof(JobObserver)} and will unsubscribe from it");
                _observer.Updated -= OnUpdated;
                _observer.Completed -= OnCompleted;
                _observer.Failed -= OnFailed;
            }
            _observer = value;
            // Subscribe to new one
            _observer.Updated += OnUpdated;
            _observer.Completed += OnCompleted;
            _observer.Failed += OnFailed;
        }
    }

    public override void _Ready()
    {
        LoadingBar = GetNode<ProgressBar>("%LoadingBar");
        Description = GetNode<Label>("%Description");
    }

    private void OnUpdated(GodotWrapper<JobMetric> jobsMetric)
    {
        var value = jobsMetric.Value;
        LoadingBar.Value = value.ProgressPercent;
        Description.Text = value.Description;
    }

    private void OnCompleted()
    {
        // TODO: Add visual indication of success
        Description.Text = "Completed";
    }

    private void OnFailed()
    {
        // TODO: Add visual indication of fail
        Description.Text = "Failed";
    }
}
