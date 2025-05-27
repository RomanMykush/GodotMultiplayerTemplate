using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Client;

public partial class Intro : Control
{
    public override void _Ready()
    {
        Logger.Singleton.Log(LogLevel.Info, "Intro started");
        GetNode<AnimationPlayer>("AnimationPlayer").Play("intro");
    }

    public override void _Input(InputEvent @event)
    {
        // Skip intro on any input
        if ((@event is InputEventKey keyEvent && keyEvent.Pressed) ||
        (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed))
            FinishIntro();
    }

    private void FinishIntro()
    {
        var node = SceneFactory.Singleton.CreateMainMenu();
        SceneTransitioner.Singleton.TryChangeScene(node);
    }
}
