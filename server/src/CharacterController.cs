using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Server;

public abstract partial class CharacterController : Node
{
    [Export] public required Character Pawn;

    public abstract void ApplyCommands(uint currentTick);
}
