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

    private readonly SortedList<uint, IEnumerable<EntityState>> Snapshots = new();
    private IEnumerable<EntityState> LastInterpolationSnapshot = new List<EntityState>();
    private readonly EntityStateComparer Comparer = new();

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
        if (Snapshots.ContainsKey(snapshot.Tick))
        {
            Snapshots.Remove(snapshot.Tick);
            Logger.Singleton.Log(LogLevel.Warning, "Snapshot with such Tick value already exists. Overwriting an old one");
        }
        Snapshots.Add(snapshot.Tick, snapshot.States);
    }

    /// <summary> Deletes old snapshots except for the most recent old snapshot. </summary>
    private void DeleteOldSnapshots(uint currentTick)
    {
        // Check if at least 2 past states exists
        if (Snapshots.Count < 2)
            return;
        var first = Snapshots.First();
        if (first.Key > currentTick)
            return;

        Snapshots.RemoveAt(0);
        do
        {
            var second = Snapshots.First();
            if (second.Key > currentTick)
                break;
            first = second;
            Snapshots.RemoveAt(0);
        } while (Snapshots.Count > 0);
        Snapshots.Add(first.Key, first.Value);
    }

    private void OnInterpolationTickUpdated(GodotWrapper<Tick> wrapper)
    {
        var tick = wrapper.Value;
        DeleteOldSnapshots(tick.CurrentTick);

        // Check if at least 2 past states exists
        if (Snapshots.Count < 2)
            return;
        var first = Snapshots.First();
        if (first.Key > tick.CurrentTick)
            return;

        // Get past and future snapshots
        uint pastTick = first.Key;
        IEnumerable<EntityState> pastSnapshot = first.Value;
        uint futureTick = 0;
        IEnumerable<EntityState> futureSnapshot = null;
        foreach (var item in Snapshots.Skip(1))
        {
            if (item.Key > tick.CurrentTick)
            {
                futureTick = item.Key;
                futureSnapshot = item.Value;
                break;
            }
            pastTick = item.Key;
            pastSnapshot = item.Value;
        }

        // Spawn and despawn entities if this is new snapshot object
        if (LastInterpolationSnapshot != pastSnapshot)
        {
            // Despawn old ones
            var despawnOnes = LastInterpolationSnapshot.Except(pastSnapshot, Comparer);
            foreach (var item in despawnOnes)
                Container.Delete(item.EntityId);

            // Spawn new ones
            var spawnOnes = pastSnapshot.Except(LastInterpolationSnapshot, Comparer);
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

        // Check if any future states exists
        if (futureSnapshot == null)
            return;

        // Get interpolation theta betwean past and future snapshots
        float tickInterval = futureTick - pastTick;
        float presentDeltaTick = tick.CurrentTick - pastTick;
        float theta = presentDeltaTick / tickInterval + tick.TickDuration * tick.TickRate / tickInterval;

        // Interpolate states
        // TODO: Add a check for equality of past and future state types in case a malicious server sends different types, what can cause a crash
        var presentSnapshot = pastSnapshot.Join(futureSnapshot, past => past.EntityId,
            future => future.EntityId, (past, future) => past.Interpolate(future, theta));

        // Apply interpolated state
        foreach (var state in presentSnapshot)
        {
            var entity = Container.Get(state.EntityId);
            entity.ApplyState(state);
        }

        LastInterpolationSnapshot = pastSnapshot;
    }

    private void OnExtrapolationTickUpdated(GodotWrapper<Tick> wrapper)
    {
        // TODO: Implement this
        // Run if there left only one old snapshot
    }

    private void OnPredictionTickUpdated(GodotWrapper<Tick> wrapper)
    {
        // TODO: Implement this
        // Get last known state and rewind simulation with known inputs
    }
}
