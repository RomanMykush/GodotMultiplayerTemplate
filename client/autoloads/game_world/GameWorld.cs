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
    private StateSnapshot LastInterpolationSnapshot = new(0, new List<EntityState>(), new List<IMeta>());
    private readonly EntityStateIdComparer EntityComparer = new();

    public override void _Ready()
    {
        Singleton = this;

        Container = GetNode<EntityContainer>("%Container");
        Container.ProcessMode = ProcessModeEnum.Disabled;

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

    private void OnInterpolationTickUpdated(GodotWrapper<Tick> wrapper)
    {
        var tick = wrapper.Value;
        DeleteOldSnapshots(tick.CurrentTick);

        // Check if at least 2 snapshots exist
        if (Snapshots.Count < 2)
            return;
        var pastSnapshot = Snapshots.Min;
        if (pastSnapshot.Tick > tick.CurrentTick)
            return;

        // Get future snapshot
        StateSnapshot futureSnapshot = null;
        foreach (var snapshot in Snapshots.Skip(1))
        {
            if (snapshot.Tick > tick.CurrentTick)
            {
                futureSnapshot = snapshot;
                break;
            }
            pastSnapshot = snapshot;
        }

        // Spawn and despawn entities if this is new snapshot object
        if (LastInterpolationSnapshot != pastSnapshot)
        {
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
        }

        // Check if any future snapshots exist
        if (futureSnapshot == null)
            return;

        // Get interpolation theta between past and future snapshots
        float tickInterval = futureSnapshot.Tick - pastSnapshot.Tick;
        float presentDeltaTick = tick.CurrentTick - pastSnapshot.Tick;
        float theta = presentDeltaTick / tickInterval + tick.TickDuration * tick.TickRate / tickInterval;

        // Interpolate states
        // TODO: Add a check for equality of past and future snapshots state types in case a malicious server sends different types, what can cause a crash
        var presentSnapshot = pastSnapshot.States.Join(futureSnapshot.States, p => p.EntityId,
            f => f.EntityId, (p, f) => p.Interpolate(f, theta));

        // Apply interpolated state
        foreach (var state in presentSnapshot)
        {
            var entity = Container.Get(state.EntityId);
            if (entity == PlayerController.Singleton.Pawn)
                continue;
            entity.ApplyState(state);
        }

        // Process metadata
        if (LastInterpolationSnapshot != pastSnapshot)
        {
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
    }

    private void OnExtrapolationTickUpdated(GodotWrapper<Tick> wrapper, float tickDelta)
    {
        // TODO: Implement this
        // Run if there left only one old snapshot
    }

    private void OnPredictionTickUpdated(GodotWrapper<Tick> wrapper, float tickDelta)
    {
        // TODO: Implement this
        // Get last known state and rewind simulation with known inputs
    }
}
