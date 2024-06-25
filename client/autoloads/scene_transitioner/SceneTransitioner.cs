using Godot;
using SteampunkDnD.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class SceneTransitioner : Node
{
    public static SceneTransitioner Singleton { get; private set; }

    // Child nodes
    private CanvasLayer WaitingLayer;
    private AnimationPlayer WaitingAnimPlayer;

    // Other properties
    private Task TransitionTask;

    public override void _Ready()
    {
        Singleton = this;
        WaitingLayer = GetNode<CanvasLayer>("%WaitingLayer");
        WaitingAnimPlayer = GetNode<AnimationPlayer>("%WaitingAnimPlayer");
    }

    /// <summary> Request scene transition. </summary>
    /// <returns>true if started transition; otherwise false.</returns>
    public bool TryChangeScene(Node node, bool skipTransition = false)
    {
        Logger.Singleton.Log(LogLevel.Trace, $"Trying to start transition to {node.GetType().Name}");
        // Check if previous transition ended
        if (TransitionTask != null && !TransitionTask.IsCompleted)
        {
            Logger.Singleton.Log(LogLevel.Warning, "Tried to start scene transition while other one in progress");
            node.QueueFree();
            return false;
        }
        // Start transition to new scene
        TransitionTask = ChangeScene(node, skipTransition);
        return true;
    }

    private async Task ChangeScene(Node node, bool skipTransition = false)
    {
        Logger.Singleton.Log(LogLevel.Trace, $"Scene transition to {node.GetType().Name} started");

        var nextLevel = node as IGameMode;
        if (nextLevel != null)
        {
            // Show waiting panel
            WaitingLayer.Show();
            WaitingAnimPlayer.Play("show_waiting_panel");

            // Pre-initialize level
            var result = await nextLevel.PreInitialize();
            if (!result.IsSuccessful)
            {
                WaitingLayer.Hide();
                _ = NotificationBox.Singleton.Show(result.Message);
                return;
            }
        }

        // Play start transition animation
        if (!skipTransition)
            await TransitionUi.Singleton.StartTransition("fade_black");

        WaitingLayer.Hide();

        // Clean up previous level
        if (GetTree().CurrentScene is IGameMode currentLevel)
            currentLevel.CleanUp();

        // Set new scene
        await GetTree().ChangeSceneToNode(node);

        // Initialize level
        if (nextLevel != null)
        {
            var jobInfos = nextLevel.ConstructInitJobs();
            if (jobInfos.Any())
            {
                // Update transition UI
                TransitionUi.Singleton.UpdateProgressBars(jobInfos);

                // Start initialization jobs
                var tasks = jobInfos.Select(ji => JobHost.Singleton.RunJob(ji.Job));

                try
                {
                    // Wait for jobs to complete
                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                    // TODO: Add more meaningful text for notification
                    await NotificationBox.Singleton.Show("Something went wrong").ContinueWith((task) =>
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

        Logger.Singleton.Log(LogLevel.Trace, $"Scene transition to {node.GetType().Name} completed");
    }
}
