using Godot;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Tests;

public partial class TestsMenu : Control
{
    [Signal] public delegate void TestSelectedEventHandler(PackedScene testScene);

    public required Dictionary<string, PackedScene> TestScenes = [];

    private Node TestButtonsHolder;

    public override void _Ready()
    {
        TestButtonsHolder = GetNode("%TestButtonsHolder");
    }

    public void UpdateTestButtons()
    {
        var buttonPackedScene = GD.Load<PackedScene>("res://tests/scenes/test_button.tscn");

        foreach (var (sceneName, testScene) in TestScenes)
        {
            var button = buttonPackedScene.Instantiate() as Button;
            button.Text = sceneName;
            TestButtonsHolder.AddChild(button);
            button.Pressed += () => EmitSignal(SignalName.TestSelected, testScene);
        }
    }
}
