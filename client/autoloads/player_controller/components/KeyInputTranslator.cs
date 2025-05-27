using Godot;
using GodotInputMapExtension;
using GodotMultiplayerTemplate.Shared;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Client;

public partial class KeyInputTranslator : Node, ICommandSource
{
    private Vector2 PreviousMoveDirection;

    public Character Pawn { get; set; }

    public Vector2 RotateByPawn(Vector2 initial)
    {
        var moveDir = new Vector3(initial.X, 0, initial.Y);
        moveDir = Pawn.GlobalBasis * moveDir;
        return new Vector2(moveDir.X, moveDir.Z);
    }

    public ICollection<ICommand> CollectCommands()
    {
        var commands = new List<ICommand>();

        // Movement
        var moveDir = Input.GetVector(InputActionHelper.Left, InputActionHelper.Right, InputActionHelper.Forward, InputActionHelper.Backward);
        moveDir = RotateByPawn(moveDir);
        var moveCmd = new MoveCommand(moveDir, PreviousMoveDirection == Vector2.Zero);
        commands.Add(moveCmd);
        PreviousMoveDirection = moveDir;

        // Jumping
        if (Input.IsActionJustPressed(InputActionHelper.Jump))
            commands.Add(new JumpCommand());

        // Todo: Add more input handling

        return commands;
    }
}
