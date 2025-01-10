using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Server;

public partial class GameWorld : Node
{
    public static GameWorld Singleton { get; private set; }

    [Signal] public delegate void SnapshotGeneratedEventHandler(GodotWrapper<StateSnapshot> wrapper);

    private EntityContainer Entities;

    public readonly List<CharacterController> Controllers = new();

    public override void _Ready()
    {
        Singleton = this;

        var physicsSpace = GetViewport().World3D.Space;
        Entities = GetNode<EntityContainer>("%Entities");
        Entities.ProcessMode = ProcessModeEnum.Disabled;
        Entities.OnAddition = (e) =>
        {
            if (e is CollisionObject3D body)
                PhysicsServer3D.BodySetSpace(body.GetRid(), physicsSpace);
        };

        TickClock.Singleton.TickUpdated += OnTickUpdated;
    }

    public void LoadLevel(Node level)
    {
        // Clear previous level
        Entities.DeleteAll();
        foreach (var node in Controllers)
        {
            if (node is PlayerController)
                continue;
            node.QueueFree();
        }
        Controllers.Clear();

        // Add new level childrens
        foreach (var child in level.GetChildren())
        {
            level.RemoveChild(child);
            switch (child)
            {
                case IEntity entity:
                    var node = entity as Node;
                    node.ProcessMode = ProcessModeEnum.Disabled;
                    Entities.Add(entity);
                    break;
                case CharacterController controller:
                    Controllers.Add(controller);
                    AddChild(controller);
                    break;
                default:
                    Logger.Singleton.Log(LogLevel.Warning, $"Unsupported type {child.GetType().Name} detected in level");
                    break;
            }
        }
    }

    private void OnTickUpdated(uint currentTick, float tickTimeDelta)
    {
        // Process character controllers
        foreach (var controller in Controllers)
            controller.ApplyCommands(currentTick);

        // Process entities
        foreach (var entity in Entities.GetAll())
        {
            var node = entity as Node;
            node._PhysicsProcess(tickTimeDelta);
        }

        // Generate snapshot
        List<EntityState> states = new();
        foreach (var entity in Entities.GetAll())
            states.Add(entity.GetState());

        // Generate meta data
        var playerControllers = Controllers
            .OfType<PlayerController>().Where(c => c.Pawn != null);
        var possesionMeta = playerControllers
            .Select(c => new PlayerPossessionMeta(c.PlayerId, c.Pawn.EntityId));

        var snapshot = new StateSnapshot(currentTick, states, possesionMeta);
        Network.Singleton.SendMessage(Network.BroadcastPeer, snapshot);
        EmitSignal(SignalName.SnapshotGenerated, new GodotWrapper<StateSnapshot>(snapshot));
    }
}
