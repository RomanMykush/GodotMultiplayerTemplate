using Godot;
using SteampunkDnD.Shared;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class SceneTransitioner : Node
{
    public static SceneTransitioner Singleton { get; private set; }
    private Task TransitionTask;

    public override void _Ready()
    {
        Singleton = this;
    }

    /// <summary> Request scene transition. </summary>
    /// <returns>true if started transition; otherwise false.</returns>
    public bool TryChangeScene(Node node, bool skipTransition = false)
    {
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
        if (!skipTransition)
            await TransitionUi.Singleton.StartTransition("fade_black");

        await GetTree().ChangeSceneToNode(node);

        if (!skipTransition)
            await TransitionUi.Singleton.FinishTransition("fade_black");
    }
}
