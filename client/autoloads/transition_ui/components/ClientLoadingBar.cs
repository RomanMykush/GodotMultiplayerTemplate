using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Client;

public partial class ClientLoadingBar : VBoxContainer
{
    // Child nodes
    private ProgressBar LoadingBar;
    private Label Description;

    // Other properties
    private ClientLoadingInfo _jobInfo;
    public ClientLoadingInfo JobInfo
    {
        get => _jobInfo;
        set
        {
            // Unsubscribe from previous observer
            if (_jobInfo != null)
            {
                Logger.Singleton.Log(LogLevel.Warning, $"An instance of {nameof(ClientLoadingBar)} was already subscribed to other {nameof(ClientLoadingInfo)} and will unsubscribe from it");
                _jobInfo.Job.Updated -= OnUpdated;
                _jobInfo.Job.Completed -= OnCompleted;
                _jobInfo.Job.Failed -= OnFailed;
            }
            _jobInfo = value;
            // Subscribe to new one
            _jobInfo.Job.Updated += OnUpdated;
            _jobInfo.Job.Completed += OnCompleted;
            _jobInfo.Job.Failed += OnFailed;
            // _jobInfo client name
            // TODO: Implement client name fetching
            GetNode<Label>("%Name").Text = _jobInfo.ClientId.ToString();
        }
    }

    public override void _Ready()
    {
        LoadingBar = GetNode<ProgressBar>("%LoadingBar");
        Description = GetNode<Label>("%Description");
    }

    private void OnUpdated(GodotWrapper<JobMetric> wrapper)
    {
        var value = wrapper.Value;
        LoadingBar.Value = value.ProgressPercent * 100;
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
