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
    public bool TryChangeScene(Node node)
    {
        // Check if previous transition ended
        if (TransitionTask != null && !TransitionTask.IsCompleted)
        {
            GD.PushWarning("Tried start scene transition while other one in progress");
            node.QueueFree();
            return false;
        }
        // Start transition to new scene
        TransitionTask = ChangeScene(node);
        return true;
    }

    private async Task ChangeScene(Node node)
    {
        await GetTree().ChangeSceneToNode(node);
    }
}
