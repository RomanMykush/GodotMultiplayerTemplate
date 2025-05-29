using Godot;
using GodotMultiplayerTemplate.Shared;
using System;

namespace GodotMultiplayerTemplate.Server;

public partial class MainGame : Node
{
    private Node3D MainBeeCube;
    private Vector3 MainBeeCubePivotPoint = new(0, 1, -10);
    private Vector3 MainBeeCubeRotationAxis = Vector3.Up;
    private float MainBeeCubeMovementSpeed = 0.002f;
    private float MainBeeCubeRotationSpeed = 10f;
    private bool BlazeManNow = false;

    public override void _Ready()
    {
        // Add bee cube to demonstate interpolation
        var beeCubeState = new StaticState(0, "BeeCube", MainBeeCubePivotPoint, Vector3.Zero);
        MainBeeCube = beeCubeState.CreateEntity() as Node3D;
        AddChild(MainBeeCube);

        GameWorld.Singleton.LoadLevel(this);

        AuthService.Singleton.PlayerJoined += (playerId) =>
        {
            Logger.Singleton.Log(LogLevel.Trace, $"Player joined {playerId}");

            // Generate character state
            string kind = BlazeManNow ? "BlazeMan" : "TriangleMan";
            BlazeManNow = !BlazeManNow;
            var randX = Random.Shared.NextSingle(-2.5f, 2.5f);
            var characterState = new CharacterState(0, kind, new Vector3(randX, 0, 0), Vector3.Zero, 0, Vector3.Zero);

            // Create character
            var character = (Character)characterState.CreateEntity();
            GameWorld.Singleton.AddEntity(character);

            // Add character controller
            var controller = new PlayerController() { PlayerId = playerId, Pawn = character };
            GameWorld.Singleton.Controllers.Add(controller);
            GameWorld.Singleton.AddChild(controller);

            Logger.Singleton.Log(LogLevel.Trace, $"Player spawned {playerId}");
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        var time = Time.GetTicksMsec();
        MainBeeCube.Position = MainBeeCube.Position with
        {
            X = Mathf.Cos(MainBeeCubeMovementSpeed * time) * 2 + MainBeeCubePivotPoint.X,
            Z = Mathf.Sin(MainBeeCubeMovementSpeed * time) * 2 + MainBeeCubePivotPoint.Z
        };
        MainBeeCube.Rotate(MainBeeCubeRotationAxis, (float)delta * MainBeeCubeRotationSpeed);
    }
}
