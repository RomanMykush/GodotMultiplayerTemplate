using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Shared;

public partial class Character : CharacterBody3D, ISpatial, IControlable
{
    [Signal] public delegate void ViewUpdatedEventHandler(Transform3D bodyTransform, Transform3D viewPointTransform);

    private Node3D ViewPoint;

    [Export] public string Kind { get; private set; }
    [Export(PropertyHint.Range, "0,90,")] private float ViewAngleСonstraint = 85;

    public uint EntityId { get; private set; }
    private IEnumerable<ICommand> LastInputs = new List<ICommand>();

    public override void _Ready()
    {
        ViewPoint = GetNode<Node3D>("%ViewPoint");
    }

    public void ReceiveCommands(IEnumerable<ICommand> commands) =>
        LastInputs = commands;

    public void UpdateViewPoint(Vector3 direction)
    {
        // NOTE: this logic will break if character up vector will be rotated
        // TODO: Make more robust for fast camera moves
        // Rotate self
        var horizontalDir = direction with { Y = 0 }; // orthogonal projection of the direction vector onto the XZ plane
        var angleTo = this.GetGlobalForward()
            .SignedAngleTo(horizontalDir, this.GetGlobalUp());
        RotateY(angleTo);

        // Rotate view point
        angleTo = ViewPoint.GetGlobalForward()
            .SignedAngleTo(direction, ViewPoint.GetGlobalRight());

        ViewPoint.RotateX(angleTo);
        ViewPoint.Rotation = ViewPoint.Rotation with
        {
            X = Mathf.Clamp(ViewPoint.Rotation.X,
                Mathf.DegToRad(-ViewAngleСonstraint),
                Mathf.DegToRad(ViewAngleСonstraint))
        };

        EmitSignal(SignalName.ViewUpdated, GlobalTransform, ViewPoint.GlobalTransform);
    }

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
