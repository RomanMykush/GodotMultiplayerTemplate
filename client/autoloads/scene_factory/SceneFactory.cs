using Godot;
using System;

namespace SteampunkDnD.Client;

public partial class SceneFactory : Node
{
    public static SceneFactory Singleton { get; private set; }
    [Export] private PackedScene _intro;
    [Export] private PackedScene _mainMenu;

    public override void _Ready() =>
        Singleton = this;

    public Node CreateIntro() =>
        _intro.Instantiate();

    public Node CreateMainMenu() =>
        _mainMenu.Instantiate();
}
