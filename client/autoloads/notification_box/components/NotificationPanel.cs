using Godot;
using System;

namespace GodotMultiplayerTemplate.Client;

public partial class NotificationPanel : Control
{
    [Signal] public delegate void ConfirmedEventHandler();
    private Label TitleLabel;
    private string _title;
    public string Title
    {
        get { return _title; }
        set
        {
            _title = value;
            TitleLabel.Text = value;
        }
    }

    public override void _Ready() =>
        TitleLabel = GetNode<Label>("%Title");

    private void Confirm() =>
        EmitSignal(SignalName.Confirmed);
}
