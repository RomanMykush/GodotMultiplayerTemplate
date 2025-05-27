using Godot;
using System;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Client;

public partial class NotificationBox : Node
{
    public static NotificationBox Singleton { get; private set; }
    private CanvasLayer NotificationLayer;
    private NotificationPanel NotificationPanel;
    public override void _Ready()
    {
        Singleton = this;
        NotificationLayer = GetNode<CanvasLayer>("%NotificationLayer");
        NotificationPanel = GetNode<NotificationPanel>("%NotificationPanel");
    }

    public async Task Show(string title)
    {
        NotificationPanel.Title = title;
        NotificationLayer.Show();
        await ToSignal(NotificationPanel, NotificationPanel.SignalName.Confirmed);
        NotificationLayer.Hide();
    }
}
