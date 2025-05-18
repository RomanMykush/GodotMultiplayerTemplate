using Godot;
using MemoryPack;
using SteampunkDnD.Shared;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Server;

public partial class GameWorld : Node
{
    public static GameWorld Singleton { get; private set; }

    [Signal] public delegate void SnapshotGeneratedEventHandler(GodotWrapper<StateSnapshot> wrapper);

    private EntityContainer Entities;

    public readonly List<CharacterController> Controllers = [];

    private readonly Dictionary<uint, StateSnapshot> SnapshotHistory = [];
    private const int MaxSnapshotHistoryCount = 100;
    private readonly Dictionary<int, uint> LastSnapshotAck = [];

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

        Network.Singleton.MessageReceived += (peer, msg) =>
        {
            if (msg.Value is StateSnapshotAck snapshotAck)
            {
                LastSnapshotAck[peer] = snapshotAck.Tick;
                ClearObsoleteSnapshots();
            }
        };

        Multiplayer.PeerDisconnected += (peer) =>
        {
            LastSnapshotAck.Remove((int)peer);
            ClearObsoleteSnapshots();
        };
    }

    private void ClearObsoleteSnapshots()
    {
        // Check if there any acknowledged snapshots
        if (LastSnapshotAck.Count == 0)
        {
            SnapshotHistory.Clear();
            return;
        }

        // Remove obsolete snapshots
        var oldestValidTick = LastSnapshotAck.Values.Min();
        var obsoleteTicks = SnapshotHistory.Keys.Where(k => k < oldestValidTick);
        foreach (var tick in obsoleteTicks)
            SnapshotHistory.Remove(tick);
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
        SnapshotHistory.Clear();

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

    private void OnTickUpdated(uint currentTick)
    {
        // Process character controllers
        foreach (var controller in Controllers)
            controller.ApplyCommands(currentTick);

        // Process entities
        float delta = 1f / Engine.PhysicsTicksPerSecond;
        foreach (var entity in Entities.GetAll())
        {
            var node = entity as Node;
            node._PhysicsProcess(delta);
        }

        // Generate snapshot
        List<EntityState> states = [];
        foreach (var entity in Entities.GetAll())
            states.Add(entity.GetState());

        // Generate meta data
        var playerControllers = Controllers
            .OfType<PlayerController>().Where(c => c.Pawn != null);
        var possesionMeta = playerControllers
            .Select(c => new PlayerPossessionMeta(c.PlayerId, c.Pawn.EntityId));

        var snapshot = new StateSnapshot(currentTick, states, possesionMeta);

        // Handle snapshot history leaking
        if (SnapshotHistory.Count > MaxSnapshotHistoryCount - 1)
        {
            Logger.Singleton.Log(LogLevel.Warning, "Snapshot history is overfilled. Removing most old ones");
            var obsoleteTicks = SnapshotHistory.Keys
                .OrderDescending().Skip(MaxSnapshotHistoryCount - 1);
            foreach (var tick in obsoleteTicks)
                SnapshotHistory.Remove(tick);
        }

        var peers = Multiplayer.GetPeers();
        if (peers.Length != 0)
        {
            SnapshotHistory[currentTick] = snapshot;
            SendSnapshotToClients(snapshot, peers);
        }

        EmitSignal(SignalName.SnapshotGenerated, new GodotWrapper<StateSnapshot>(snapshot));
    }

    private void SendSnapshotToClients(StateSnapshot snapshot, int[] peers)
    {
        byte[] originalData = MemoryPackSerializer.Serialize(snapshot);
        // Send snapshot to each client
        foreach (var peer in peers)
        {
            // Check if this is first snapshot for client
            if (!LastSnapshotAck.TryGetValue(peer, out uint baselineTick))
            {
                Network.Singleton.SendMessage(peer, snapshot);
                continue;
            }
            // Check if snapshot exists
            if (!SnapshotHistory.TryGetValue(baselineTick, out var baseline))
            {
                Logger.Singleton.Log(LogLevel.Warning, "Acknowledged by client snapshot does not exist in snapshot history");
                Network.Singleton.SendMessage(peer, snapshot);
                continue;
            }

            var deltaSnapshot = StateSnapshotUtils.DeltaEncode(baseline, snapshot);

            // Check if delta snapshot is less then original
            byte[] deltaData = MemoryPackSerializer.Serialize(deltaSnapshot);
            if (deltaData.Length > originalData.Length)
            {
                Network.Singleton.SendMessage(peer, snapshot);
                continue;
            }

            Network.Singleton.SendMessage(peer, deltaSnapshot);
        }
    }
}
