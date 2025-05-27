using Godot;

namespace GodotMultiplayerTemplate.Shared;

public static class NodeUtils
{
    public static Vector3 GetGlobalForward(this Node3D node) =>
        -node.GlobalBasis.Z;

    public static Vector3 GetGlobalUp(this Node3D node) =>
        node.GlobalBasis.Y;

    public static Vector3 GetGlobalRight(this Node3D node) =>
        node.GlobalBasis.X;

    public static void ClearChildren(this Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
