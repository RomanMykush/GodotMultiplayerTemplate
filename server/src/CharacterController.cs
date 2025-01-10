using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Server;

public abstract partial class CharacterController : Node
{
    // TODO: Make property required after migration to .NET 8
    [Export] public Character Pawn;

    public abstract void ApplyCommands(uint currentTick);
}
