using Godot;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Client;

public partial class MessageBox : Node
{
    public static MessageBox Singleton { get; private set; }
    private CanvasLayer MessageLayer;
    private MessagePanel MessagePanel;
    public override void _Ready()
    {
        Singleton = this;
        MessageLayer = GetNode<CanvasLayer>("%MessageLayer");
        MessagePanel = GetNode<MessagePanel>("%MessagePanel");
    }

    public async Task Show(string title)
    {
        MessagePanel.Title = title;
        MessageLayer.Show();
        await ToSignal(MessagePanel, MessagePanel.SignalName.Confirmed);
        MessageLayer.Hide();
    }
}
