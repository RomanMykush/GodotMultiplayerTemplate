using Godot;
using System;

namespace SteampunkDnD.Client;

public partial class Spinner : TextureProgressBar
{
    [Export] public float RotationSpeed = 1.25f;
    public override void _Ready()
    {
        var tween = GetTree().CreateTween().SetLoops();
        tween.TweenProperty(this, "radial_initial_angle", 360, 1 / RotationSpeed).AsRelative();
    }
}
