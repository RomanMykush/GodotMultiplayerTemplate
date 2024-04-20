using Godot;
using System;

namespace SteampunkDnD.Shared;

public abstract partial class PlatformStartUp : Node
{
    [Export] public int PhysicsTicksRate;
    [Export] public Godot.Collections.Array<PackedScene> Singletons { get; set; }

    public override sealed void _Ready()
    {
        // Configure engine
        Engine.PhysicsTicksPerSecond = PhysicsTicksRate;
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
