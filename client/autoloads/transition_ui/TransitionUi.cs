using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class TransitionUi : Node
{
    public static TransitionUi Singleton { get; private set; }

    // Exports
    [Export] private string DefaultAnimation;
    [Export] private PackedScene ClientBarScene;
    [Export] private PackedScene LevelBarScene;

    // Child nodes
    private CanvasLayer TransitionLayer;
    private Control ClientBarsContainer;
    private Control LoadingBarsContainer;
    private AnimationPlayer TransitionAnimPlayer;
    private AnimationPlayer ProgressBarsAnimPlayer;
    private Timer ProgressShowingDelay;


    public override void _Ready()
    {
        Singleton = this;
        // Get references to childs
        TransitionLayer = GetNode<CanvasLayer>("%TransitionLayer");
        ClientBarsContainer = GetNode<Control>("%ClientBarsContainer");
        LoadingBarsContainer = GetNode<Control>("%LoadingBarsContainer");
        TransitionAnimPlayer = GetNode<AnimationPlayer>("%TransitionAnimPlayer");
        ProgressBarsAnimPlayer = GetNode<AnimationPlayer>("%ProgressBarsAnimPlayer");
        ProgressShowingDelay = GetNode<Timer>("%ProgressShowingDelay");

        // Play progress bar showing animation on timer expiration
        ProgressShowingDelay.Timeout += () => ProgressBarsAnimPlayer.Play("slide_in_vertically");
    }

    public async Task StartTransition(string animationName)
    {
        TransitionLayer.Show();

        // Play start transition animation
        await TransitionAnimation(animationName);
    }

    public async Task FinishTransition(string animationName)
    {
        // Play end transition animation
        await TransitionAnimation(animationName, true);

        TransitionLayer.Hide();
        ClearProgressBars();
    }

    public void UpdateProgressBars(IEnumerable<JobObserver> jobObservers)
    {
        ClearProgressBars();

        // Add progress bar for each passed JobObserver
        foreach (var observer in jobObservers)
        {
            switch (observer)
            {
                case LoadingObserver:
                case GeneratorObserver:
                    var loadingBar = LevelBarScene.Instantiate<LevelLoadingBar>();
                    LoadingBarsContainer.AddChild(loadingBar);
                    loadingBar.Observer = observer;
                    break;
                case ClientLoadingObserver clientLoadingObserver:
                    var cleintLoadingBar = ClientBarScene.Instantiate<ClientLoadingBar>();
                    ClientBarsContainer.AddChild(cleintLoadingBar);
                    cleintLoadingBar.Observer = clientLoadingObserver;
                    break;
                default:
                    Logger.Singleton.Log(LogLevel.Warning, $"Unsupported {observer.GetType().Name} subtype of JobObserver was passed");
                    break;
            }
        }
    }

    private void ClearProgressBars()
    {
        ClientBarsContainer.ClearChildren();
        LoadingBarsContainer.ClearChildren();
    }

    private async Task TransitionAnimation(string animationName, bool inverse = false)
    {
        if (!TransitionAnimPlayer.HasAnimation(animationName))
        {
            Logger.Singleton.Log(LogLevel.Warning, $"{animationName} animation does not exist");
            animationName = DefaultAnimation;
        }

        // Hide bars on end transition
        if (inverse)
        {
            // Check if bars have been shown
            if (ProgressShowingDelay.Paused)
            {
                ProgressBarsAnimPlayer.PlayBackwards("slide_in_vertically");
                // Wait for bars hiding animation to finish
                await ToSignal(ProgressBarsAnimPlayer,
                    AnimationPlayer.SignalName.AnimationFinished);
            }
            // Stop bars showing timer
            ProgressShowingDelay.Stop();
        }

        TransitionAnimPlayer.Play(animationName,
            customSpeed: inverse ? -1 : 1, fromEnd: inverse);
        // Wait for start/end transition animation to finish
        await ToSignal(TransitionAnimPlayer,
            AnimationPlayer.SignalName.AnimationFinished);

        // Show bars after timer timeout
        if (!inverse)
            ProgressShowingDelay.Start();
    }
}
