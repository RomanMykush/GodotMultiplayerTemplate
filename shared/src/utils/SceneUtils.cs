using Godot;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public static class SceneUtils
{
    public async static Task ChangeSceneToNode(this SceneTree tree, Node node)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.CurrentScene.Free();
        tree.Root.AddChild(node);
        tree.CurrentScene = node;
    }
}
