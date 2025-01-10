using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Shared;

public partial class Character : CharacterBody3D, ISpatial, IControlable
{
    [Export] public string Kind { get; private set; }

    public uint EntityId { get; private set; }
    private IEnumerable<ICommand> LastInputs = new List<ICommand>();

    public void ReceiveCommands(IEnumerable<ICommand> commands) =>
        LastInputs = commands;

    public override void _PhysicsProcess(double delta)
    {
        // TODO: Add physics and command processing

        // Get all continuous inputs and mark them as not started recenty
        LastInputs = LastInputs.OfType<ContiniousCommand>()
            .Select(i => i with { JustStarted = false });
    }

    public EntityState GetState() => new CharacterState(EntityId, Kind, Position, Rotation, Velocity);

    public void ApplyState(EntityState state)
    {
        if (state is not CharacterState characterState)
            throw new ArgumentException("Invalid argument type was passed");

        if (EntityId == 0)
            EntityId = characterState.EntityId;
        else if (EntityId != characterState.EntityId)
            throw new ArgumentException("State with wrong Id was passed");

        if (Kind != characterState.Kind)
            Logger.Singleton.Log(LogLevel.Error, "State with wrong Kind was passed");

        Position = characterState.Position;
        Rotation = characterState.Rotation;
        Velocity = characterState.Velocity;
    }
}
