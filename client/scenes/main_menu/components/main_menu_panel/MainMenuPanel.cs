using Godot;
using GodotMultiplayerTemplate.Shared;
using System;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Client;

public partial class MainMenuPanel : Control
{
    // Exports
    [Export] private PackedScene HostGameMenuScene;
    [Export] private PackedScene JoinGameMenuScene;

    // Child nodes
    private Control ContentContainer;
    private AnimationPlayer SidePanelAnimPlayer;
    private AnimationPlayer ContentAnimPlayer;

    // Other properties
    private PackedScene PendingScene;
    private bool SidePanelOpened = false;
    private Task ContentRequest;

    public override void _Ready()
    {
        Logger.Singleton.Log(LogLevel.Info, "Main menu opened");
        ContentContainer = GetNode<Control>("%ContentContainer");
        SidePanelAnimPlayer = GetNode<AnimationPlayer>("%SidePanelAnimPlayer");
        ContentAnimPlayer = GetNode<AnimationPlayer>("%ContentAnimPlayer");
    }

    public void RequestContent(string sceneName)
    {
        PendingScene = sceneName switch
        {
            nameof(HostGameMenu) => HostGameMenuScene,
            nameof(JoinGameMenu) => JoinGameMenuScene,
            _ => throw new NotImplementedException("Subscene with such name was not found")
        };

        // Check if side panel closed
        if (!SidePanelOpened)
        {
            SidePanelOpened = true;
            SidePanelAnimPlayer.Play("open_side_panel");
            UpdateContent();
            return;
        }

        // Check if previous task ended
        if (ContentRequest == null
            || ContentRequest.IsCompleted)
        {
            ContentAnimPlayer.PlayBackwards("show_content");
            // Create waiting task
            var waiter = ContentAnimPlayer.ToSignal(
                    ContentAnimPlayer,
                    AnimationMixer.SignalName.AnimationFinished);
            // Update on animation finished
            ContentRequest = Task.Run(async () =>
            {
                await waiter;
                UpdateContent();
            });
        }
    }

    public void UpdateContent()
    {
        // Remove previous subscene
        foreach (var child in ContentContainer.GetChildren())
            child.QueueFree();

        // Add pending subscene
        var scene = PendingScene.Instantiate();
        ContentContainer.AddChild(scene);

        // Play openning animation
        ContentAnimPlayer.Play("show_content");
    }

    private void OnExit() => AppManager.Singleton.Exit();
}
