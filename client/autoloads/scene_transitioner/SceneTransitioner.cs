using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class SceneTransitioner : Node
{
    public static SceneTransitioner Singleton { get; private set; }

    // Child nodes
    private Node JobObserversContainer;
    private CanvasLayer WaitingLayer;
    private AnimationPlayer WaitingAnimPlayer;

    // Other properties
    private Task TransitionTask;

    public override void _Ready()
    {
        Singleton = this;
        JobObserversContainer = GetNode("%JobObserversContainer");
        WaitingLayer = GetNode<CanvasLayer>("%WaitingLayer");
        WaitingAnimPlayer = GetNode<AnimationPlayer>("%WaitingAnimPlayer");
    }

    /// <summary> Request scene transition. </summary>
    /// <returns>true if started transition; otherwise false.</returns>
    public bool TryChangeScene(Node node, bool skipTransition = false)
    {
        GD.Print($"Trying to start transition to {node.GetType().Name}");
        // Check if previous transition ended
        if (TransitionTask != null && !TransitionTask.IsCompleted)
        {
            GD.PushWarning("Tried start scene transition while other one in progress");
            node.QueueFree();
            return false;
        }
        // Start transition to new scene
        TransitionTask = ChangeScene(node, skipTransition);
        return true;
    }

    private async Task ChangeScene(Node node, bool skipTransition = false)
    {
        GD.Print($"Scene transition to {node.GetType().Name} started");

        var nextLevel = node as ILevel;
        if (nextLevel != null)
        {
            // Show waiting panel
            WaitingLayer.Show();
            WaitingAnimPlayer.Play("show_waiting_panel");

            // Initialize level
            var result = await nextLevel.Initialize();
            if (!result.IsSuccessful)
            {
                WaitingLayer.Hide();
                _ = MessageBox.Singleton.Show(result.Message);
                return;
            }
        }

        // Play start transition animation
        if (!skipTransition)
            await TransitionUi.Singleton.StartTransition("fade_black");

        WaitingLayer.Hide();

        // Clean up previous level
        if (GetTree().CurrentScene is ILevel currentLevel)
            currentLevel.CleanUp();

        // Set new scene
        await GetTree().ChangeSceneToNode(node);

        // Start level construction and wait for its success or fail
        if (nextLevel != null)
        {
            var observers = nextLevel.StartConstruction();
            if (observers.Any())
            {
                foreach (var observer in observers)
                    JobObserversContainer.AddChild(observer);

                // Update transition UI
                TransitionUi.Singleton.UpdateProgressBars(observers);

                // Start monitoring jobs completion
                var successTasks = new List<Task>();
                var failTasks = new List<Task>();
                foreach (var observer in observers) // can potentially cause task memory leak as some will never be finished, but will lose all strong references so i don't know exactly...
                {
                    successTasks.Add(Task.Run(async () => await ToSignal(observer, JobObserver.SignalName.Completed)));
                    failTasks.Add(Task.Run(async () => await ToSignal(observer, JobObserver.SignalName.Failed)));
                }

                // Wait for jobs to complete
                int index = Task.WaitAny(Task.WhenAll(successTasks), Task.WhenAny(failTasks));
                // On fail, redirect player to main menu
                if (index == 1)
                {
                    await MessageBox.Singleton.Show("Something went wrong").ContinueWith((task) =>
                    {
                        var node = SceneFactory.Singleton.CreateMainMenu();
                        TransitionTask = ChangeScene(node);
                    });
                    return;
                }
            }
        }

        // Play end transition animation
        if (!skipTransition)
            await TransitionUi.Singleton.FinishTransition("fade_black");

        GD.Print($"Scene transition to {node.GetType().Name} completed");
    }
}
