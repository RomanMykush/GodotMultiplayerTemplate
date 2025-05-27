using Godot;
using System;

namespace GodotMultiplayerTemplate.Shared;

public abstract partial class PlatformStartUp : Node
{
    [Export] private Godot.Collections.Array<PackedScene> Singletons { get; set; }

    public override sealed void _Ready()
    {
        // Spawn autoloads
        var root = GetTree().Root;
        foreach (var scene in Singletons)
        {
            var node = scene.Instantiate();
            root.CallDeferred("add_child", node);
        }
        CallDeferred(MethodName.AfterReady);
    }

    public abstract void AfterReady();
}
