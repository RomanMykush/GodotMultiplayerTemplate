using Godot;
using System;

namespace SteampunkDnD.Client;

public partial class JoinGameMenu : MarginContainer
{
    private LineEdit IpEdit;
    public override void _Ready() =>
        IpEdit = GetNode<LineEdit>("%IpEdit");

    private void JoinGame()
    {
        // TODO: Implement client creation
    }
}
