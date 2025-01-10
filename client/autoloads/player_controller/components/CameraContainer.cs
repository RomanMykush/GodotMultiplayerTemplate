using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;

namespace SteampunkDnD.Client;

public partial class CameraContainer : Node3D, ICommandSource
{
    private Camera3D MainCamera;

    private Character _pawn;
    public Character Pawn
    {
        get => _pawn;
        set
        {
            if (IsInstanceValid(_pawn))
            {
                _pawn.Visible = true;
                _pawn.ViewUpdated -= OnViewUpdated;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            _pawn = value;
            PawnRid = new() { _pawn.GetRid() };

            if (IsInstanceValid(_pawn))
            {
                _pawn.Visible = false;
                _pawn.ViewUpdated += OnViewUpdated;
                Input.MouseMode = Input.MouseModeEnum.Captured;
                // Update camera view
                _pawn.UpdateViewPoint(_pawn.GetGlobalForward());
            }
        }
    }
    private const float MaxViewTargetDistance = 100;

    // TODO: Implement settings menu
    // Options properties
    private float Sensitivity = 0.002f;

    // Cached values
    private Godot.Collections.Array<Rid> PawnRid;

    public override void _Ready()
    {
        MainCamera = GetNode<Camera3D>("%MainCamera");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
            return;

        if (@event is InputEventMouseMotion mouseMoveEvent)
        {
            MainCamera.RotateX(-mouseMoveEvent.Relative.Y * Sensitivity);
            RotateY(-mouseMoveEvent.Relative.X * Sensitivity);

            Pawn.UpdateViewPoint(MainCamera.GetGlobalForward());
        }
    }

    public override void _Process(double delta)
    {
        // TODO: Implement head bob and motion fov
    }

    private Vector3 CalculateViewTarget()
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        var distantViewPoint = GlobalPosition + this.GetGlobalForward() * MaxViewTargetDistance;
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, distantViewPoint, exclude: PawnRid);
        var result = spaceState.IntersectRay(query);

        // Check if there is any collision
        if (result.Count <= 0)
            return distantViewPoint;

        return result["position"].AsVector3();
    }

    public ICollection<ICommand> CollectCommands()
    {
        var viewTarget = CalculateViewTarget();
        var command = new LookAtCommand(viewTarget);
        return new List<ICommand>() { command };
    }

    private void OnViewUpdated(Transform3D bodyTransform, Transform3D viewPointTransform)
    {
        GlobalTransform = bodyTransform;
        MainCamera.GlobalTransform = viewPointTransform;
    }
}
