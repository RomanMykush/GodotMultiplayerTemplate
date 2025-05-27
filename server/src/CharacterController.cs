using Godot;
using SteampunkDnD.Shared;

namespace SteampunkDnD.Server;

public abstract partial class CharacterController : Node
{
    [Export] public required Character Pawn;

    public abstract void ApplyCommands(uint currentTick);
}
