using Godot;
using SteampunkDnD.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Client;

public partial class GameWorld : Node
{
    public static GameWorld Singleton { get; private set; }

    private EntityContainer Container;

    private readonly SortedSet<StateSnapshot> Snapshots = new(new StateSnapshotTickComparer());
    private StateSnapshot LastInterpolationSnapshot = new(0, [], []);
    private uint LastInterpolationTick;
    private StateSnapshot LastPredictionSnapshot = new(0, [], []);
    private List<uint> PredictionTicks = [];
    private readonly EntityStateIdComparer EntityComparer = new();

    public override void _Ready()
    {
        Singleton = this;

        var physicsSpace = GetViewport().World3D.Space;
        Container = GetNode<EntityContainer>("%Container");
        Container.ProcessMode = ProcessModeEnum.Disabled;
        Container.OnAddition = (e) =>
        {
            if (e is CollisionObject3D body)
                PhysicsServer3D.BodySetSpace(body.GetRid(), physicsSpace);
        };

        TickClock.Singleton.InterpolationTickUpdated += OnInterpolationTickUpdated;
        TickClock.Singleton.ExtrapolationTickUpdated += OnExtrapolationTickUpdated;
        TickClock.Singleton.PredictionTickUpdated += OnPredictionTickUpdated;

        Network.Singleton.MessageReceived += (msg) =>
        {
            if (msg.Value is StateSnapshot snapshot)
                OnSnapshotReceived(snapshot);
        };
    }

    private void OnSnapshotReceived(StateSnapshot snapshot)
    {
        // Remove existing snapshot with same Tick value
        if (Snapshots.Remove(snapshot))
            Logger.Singleton.Log(LogLevel.Warning, "Snapshot with such Tick value already exists. Overwriting an old one");

        Snapshots.Add(snapshot);
    }

    /// <summary> Deletes old snapshots except for the most recent old snapshot. </summary>
    private void DeleteOldSnapshots(uint currentTick)
    {
        // Check if at least 2 snapshots exist
        if (Snapshots.Count < 2)
            return;
        var first = Snapshots.Min;
        if (first.Tick > currentTick)
            return;

        Snapshots.Remove(Snapshots.Min);
        do
        {
            var second = Snapshots.Min;
            if (second.Tick > currentTick)
                break;
            first = second;
            Snapshots.Remove(first);
        } while (Snapshots.Count > 0);
        Snapshots.Add(first);
    }

    private IEnumerable<EntityState> GetInterpolatedPresentStates(uint currentTick)
    {
        var pastSnapshot = Snapshots.Min;
        if (pastSnapshot.Tick == currentTick)
            return pastSnapshot.States;

        // Get next to past snapshot
        StateSnapshot futureSnapshot = null;
        foreach (var snapshot in Snapshots.Skip(1))
        {
            if (snapshot.Tick > currentTick)
            {
                futureSnapshot = snapshot;
                break;
            }
            pastSnapshot = snapshot;
        }
        // Check if any future snapshots exist
        if (futureSnapshot == null)
            return null;

        // Get interpolation theta between past and future snapshots
        float theta = (currentTick - pastSnapshot.Tick) / (futureSnapshot.Tick - pastSnapshot.Tick);

        // Interpolate states
        // TODO: Add a check for equality of past and future snapshots state types in case a malicious server sends different types, what can cause a crash
        return pastSnapshot.States.Join(futureSnapshot.States, p => p.EntityId,
            f => f.EntityId, (p, f) => p.Interpolate(f, theta));
    }

    private void OnInterpolationTickUpdated(uint currentTick)
    {
        DeleteOldSnapshots(currentTick);

        // Check if at least 2 snapshots exist
        if (Snapshots.Count < 2)
            return;
        var pastSnapshot = Snapshots.Min;
        if (pastSnapshot.Tick > currentTick)
            return;

        if (LastInterpolationTick == currentTick)
            return;

        if (LastInterpolationSnapshot != pastSnapshot)
        {
            // Spawn and despawn entities if this is new snapshot object
            // Despawn old ones
            var despawnOnes = LastInterpolationSnapshot.States.Except(pastSnapshot.States, EntityComparer);
            foreach (var item in despawnOnes)
                Container.Delete(item.EntityId);

            // Spawn new ones
            var spawnOnes = pastSnapshot.States.Except(LastInterpolationSnapshot.States, EntityComparer);
            foreach (var item in spawnOnes)
            {
                try
                {
                    var entity = item.CreateEntity();
                    Container.Add(entity);
                }
                catch (Exception e)
                {
                    Logger.Singleton.Log(LogLevel.Error, e.Message);
                }
        }

        // Process metadata
            foreach (var meta in pastSnapshot.MetaData)
            {
                switch (meta)
                {
                    case PlayerPossessionMeta playerPossession:
                        if (playerPossession.PlayerId == AuthService.Singleton.PlayerId)
                        {
                            // TODO: Add check if entity exists
                            if (!Container.Contains(playerPossession.EntityId))
                            {
                                Logger.Singleton.Log(LogLevel.Error, "Server sent entity id for possession of non-existing character");
                                break;
                            }
                            var character = (Character)Container.Get(playerPossession.EntityId);
                            PlayerController.Singleton.Pawn = character;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        LastInterpolationSnapshot = pastSnapshot;

        var presentStates = GetInterpolatedPresentStates(currentTick);
        if (presentStates == null)
            return;
        LastInterpolationTick = currentTick;

        // Apply interpolated state
        foreach (var state in presentStates)
        {
            var entity = Container.Get(state.EntityId);
            if (entity == PlayerController.Singleton.Pawn)
                continue;
            entity.ApplyState(state);
        }
    }

    private void OnExtrapolationTickUpdated(uint currentTick)
    {
        // TODO: Implement this
        // Run if there left only one old snapshot
    }

    private void ApplyPrediction(uint snapshotTick, EntityState initialState, Character pawn, PlayerController controller)
    {
        if (initialState == null)
            return;
        pawn.ApplyState(initialState);

        var relevantTicks = PredictionTicks.Where(t => t > snapshotTick);
        if (!relevantTicks.Any())
            return;

        // Rewind simulation for rest of ticks
        float delta = 1f / Engine.PhysicsTicksPerSecond;
        foreach (var tick in relevantTicks)
        {
            if (controller.TryGetCommands(tick, out var commands))
                pawn.ReceiveCommands(commands);

            // Simulate character
            pawn.ManualProcess(delta);
            pawn.ManualPhysicsProcess(delta);
        }
    }

    private void OnPredictionTickUpdated(uint currentTick)
    {
        if (Snapshots.Count < 1)
            return;

        // Check if next prediction tick is less then previous one(s)
        int index = PredictionTicks.FindIndex((t) => t >= currentTick);
        if (index != -1)
        {
            Logger.Singleton.Log(LogLevel.Warning, "New prediction tick is less then previous one(s). Removing commands associated with newer ticks");

            // Extract all invalid ticks
            var invalidTicks = PredictionTicks
                .GetRange(index, PredictionTicks.Count - index);
            // Leave all relevant ticks
            PredictionTicks = PredictionTicks.GetRange(0, index);
            // Remove old commands
            foreach (var tick in invalidTicks)
                PlayerController.Singleton.RemoveCommands(tick);
        }

        // Check if there is new latest snapshot
        var latestSnapshot = Snapshots.Max;
        if (LastPredictionSnapshot != latestSnapshot)
        {
            // Get last outdated tick
            index = PredictionTicks.FindLastIndex((t) => t <= latestSnapshot.Tick);
            if (index != -1)
            {
                // Extract all outdated ticks
                var oldTicks = PredictionTicks.GetRange(0, index + 1);
                // Leave all relevant ticks
                PredictionTicks = PredictionTicks
                    .GetRange(index + 1, PredictionTicks.Count - (index + 1));
                // Remove old commands and deltas
                foreach (var tick in oldTicks)
                    PlayerController.Singleton.RemoveCommands(tick);
            }

            if (PlayerController.Singleton.Pawn != null)
            {
                var characterState = latestSnapshot.States
                    .FirstOrDefault(s => s.EntityId == PlayerController.Singleton.Pawn.EntityId);

                ApplyPrediction(latestSnapshot.Tick, characterState, PlayerController.Singleton.Pawn, PlayerController.Singleton);
            }
        }
        LastPredictionSnapshot = latestSnapshot;

        PredictionTicks.Add(currentTick);

        // Collect player input
        var collectedCommands = PlayerController.Singleton.CollectCommands(currentTick);

        // Generating pending commands
        var pendingCommands = new List<CommandSnapshot>(PredictionTicks.Count);
        foreach (var tick in PredictionTicks)
            if (PlayerController.Singleton.TryGetCommands(tick, out var commands))
                pendingCommands.Add(new CommandSnapshot(tick, commands));

        // Send all unprocessed by server commands
        var recentCommands = new RecentCommandSnapshots(pendingCommands);
        Network.Singleton.SendMessage(recentCommands);

        // Simulate character
        if (PlayerController.Singleton.Pawn != null)
        {
            PlayerController.Singleton.Pawn.ReceiveCommands(collectedCommands);
            float delta = 1f / Engine.PhysicsTicksPerSecond;
            PlayerController.Singleton.Pawn.ManualPhysicsProcess(delta);
        }
    }
}
