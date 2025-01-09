using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;

namespace SteampunkDnD.Server;

public partial class GameWorld : Node
{
    public static GameWorld Singleton { get; private set; }

    [Signal] public delegate void SnapshotGeneratedEventHandler(GodotWrapper<StateSnapshot> wrapper);

    private EntityContainer Entities;

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
                default:
                    Logger.Singleton.Log(LogLevel.Warning, $"Unsupported type {child.GetType().Name} detected in level");
                    break;
            }
        }
    }

    private void OnTickUpdated(uint currentTick, float tickTimeDelta)
    {
        // TODO: Add player and AI input processing

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

        var snapshot = new StateSnapshot(currentTick, states);
        Network.Singleton.SendMessage(Network.BroadcastPeer, snapshot);
        EmitSignal(SignalName.SnapshotGenerated, new GodotWrapper<StateSnapshot>(snapshot));
    }
}
