using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotMultiplayerTemplate.Shared;

public partial class Character : CustomCharacterBody3D, ISpatial, IControlable
{
    [Signal] public delegate void ViewUpdatedEventHandler(Vector3 viewPosition);

    public Node3D ViewPoint { get; private set; }

    [Export] public string Kind { get; private set; }
    [Export(PropertyHint.Range, "0,90,")] public float ViewAngleConstraint { get; private set; } = 85;

    public uint EntityId { get; set; }
    private IEnumerable<ICommand> LastInputs = [];

    // TODO: Put those properties in a separate physics logic components
    [Export] public float Gravity { get; private set; } = 10;
    [Export] public float JumpVelocity { get; private set; } = 10;
    [Export] public float WalkingSpeed { get; private set; } = 10;
    [Export] public float WalkingStrength { get; private set; } = 60;

    public void LoadChildren()
    {
        ViewPoint = GetNode<Node3D>("%ViewPoint");
    }

    public override void _Ready()
    {
        LoadChildren();
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

        float viewRotation = Mathf.Clamp(ViewPoint.Rotation.X + angleTo,
            Mathf.DegToRad(-ViewAngleConstraint), Mathf.DegToRad(ViewAngleConstraint));
        ViewPoint.Rotation = new Vector3(viewRotation, 0, 0);

        EmitSignal(SignalName.ViewUpdated, ViewPoint.GlobalPosition);
    }

    public void ManualProcess(double delta)
    {
        // TODO: Add more logic
        _Process(delta);
    }

    private Vector3 CalculateVelocity(double delta)
    {
        float floatDelta = (float)delta;
        // TODO: Refactor this to allow other movement and physics types
        Vector3 resultVelocity = Velocity;

        bool onFloorIfSnapped = OnFloorIfSnapped();
        if (!onFloorIfSnapped)
            resultVelocity.Y -= Gravity * floatDelta;

        if (LastInputs.Any(c => c is JumpCommand) && onFloorIfSnapped)
            resultVelocity.Y += JumpVelocity;

        var moveCommand = LastInputs.FirstOrDefault(c => c is MoveCommand) as MoveCommand;
        var inputDir = moveCommand != null ? moveCommand.Direction : Vector2.Zero;
        var direction = new Vector3(inputDir.X, 0, inputDir.Y);

        var targetWalkingVelocity = direction * WalkingSpeed;
        var counteringVelocityDelta = targetWalkingVelocity - resultVelocity;
        counteringVelocityDelta.Y = 0;
        if (WalkingStrength * WalkingStrength * floatDelta * floatDelta > counteringVelocityDelta.LengthSquared())
            resultVelocity = targetWalkingVelocity with { Y = resultVelocity.Y };
        else resultVelocity += counteringVelocityDelta.Normalized() * WalkingStrength * floatDelta;

        return resultVelocity;
    }

    private void AdvancePhysics(double delta)
    {
        // Rotate character toward view target
        var lookCommand = LastInputs.FirstOrDefault(c => c is LookAtCommand) as LookAtCommand;
        if (lookCommand != null)
            UpdateViewPoint(ViewPoint.GlobalPosition.DirectionTo(lookCommand.Target));

        Move(delta);

        // Get all continuous inputs and mark them as not started recenty
        LastInputs = LastInputs.OfType<ContiniousCommand>()
            .Select(i => i with { JustStarted = false });
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = CalculateVelocity(delta);
        AdvancePhysics(delta);
    }

    public void ManualPhysicsProcess(double delta)
    {
        Velocity = CalculateVelocity(delta);
        AdvancePhysics(delta);
    }

    public EntityState GetState() => new CharacterState(EntityId, Kind, Position, Rotation, ViewPoint.Rotation.X, Velocity);

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
        ViewPoint.Rotation = new Vector3(characterState.ViewRotation, 0, 0);
        Velocity = characterState.Velocity;
    }
}
