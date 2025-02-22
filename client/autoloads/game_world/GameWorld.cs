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
    private StateSnapshot LastPredictionSnapshot = new(0, new List<EntityState>(), new List<IMeta>());
    private List<MarkedValue<SoftTick>> PredictionTicks = new(); // sorted list of prediction ticks with their ids
    private readonly Dictionary<uint, float> PredictionTickDeltas = new(); // dictionary of tick ids with their physics delta times
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

    private void OnInterpolationTickUpdated(GodotWrapper<SoftTick> wrapper)
    {
        var tick = wrapper.Value;
        DeleteOldSnapshots(tick.TickCount);

        // Check if at least 2 snapshots exist
        if (Snapshots.Count < 2)
            return;
        var pastSnapshot = Snapshots.Min;
        if (pastSnapshot.Tick > tick.TickCount)
            return;

        // Get future snapshot
        StateSnapshot futureSnapshot = null;
        foreach (var snapshot in Snapshots.Skip(1))
        {
            if (snapshot.Tick > tick.TickCount)
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
        float snapshotsDeltaTick = futureSnapshot.Tick - pastSnapshot.Tick;
        float presentDeltaTick = tick.TickCount - pastSnapshot.Tick;
        float theta = (presentDeltaTick + tick.TickDuration * tick.TickRate) / snapshotsDeltaTick;

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

    private void OnExtrapolationTickUpdated(GodotWrapper<SoftTick> wrapper, float tickDelta)
    {
        // TODO: Implement this
        // Run if there left only one old snapshot
    }

    private void CheckTickRateChange(SoftTick newTick)
    {
        if (PredictionTicks.Count < 1)
            return;

        var originalTickRate = PredictionTicks.First().Value.TickRate;
        if (originalTickRate != newTick.TickRate)
        {
            // TODO: Implement commands translation into new tick rate
            Logger.Singleton.Log(LogLevel.Warning, "New tickrate detected. Removing all commands");

            // TEMP solution: Remove all commands and deltas
            foreach (var invalidTick in PredictionTicks)
                PlayerController.Singleton.RemoveCommands(invalidTick.Id);
            PredictionTickDeltas.Clear();
        }
    }

    private void ApplyPrediction(uint snapshotTick, EntityState initialState, Character pawn, PlayerController controller)
    {
        if (initialState == null)
            return;
        pawn.ApplyState(initialState);

        var relevantTicks = PredictionTicks.Where(m => m.Value.TickCount >= snapshotTick);
        if (!relevantTicks.Any())
            return;

        // Rewind simulation for first prediction tick after snapshot
        var firstTick = relevantTicks.First();
        if (controller.TryGetCommands(firstTick.Id, out var commands))
            pawn.ReceiveCommands(commands);

        float delta = Math.Min(firstTick.Value.TickDuration, PredictionTickDeltas[firstTick.Id]);
        pawn.ManualProcess(delta);
        pawn.ManualPhysicsProcess(delta);

        // Rewind simulation for rest of ticks
        foreach (var markedTick in relevantTicks.Skip(1))
        {
            if (controller.TryGetCommands(markedTick.Id, out commands))
                pawn.ReceiveCommands(commands);

            // Simulate character
            delta = PredictionTickDeltas[markedTick.Id];
            pawn.ManualProcess(delta);
            pawn.ManualPhysicsProcess(delta);
        }
    }

    private uint GenerateTickId()
    {
        uint current = (uint)Random.Shared.Next();
        while (PredictionTicks.Any(k => k.Id == current))
            current++;
        return current;
    }

    private static Dictionary<uint, List<(uint id, SoftTick tick, float delta)>> DivideByServerTicks(List<MarkedValue<SoftTick>> clientTicks, Dictionary<uint, float> clientTickDeltas)
    {
        var splitedClientTicks = new Dictionary<uint, List<(uint id, SoftTick tick, float delta)>>();
        foreach (var (id, tick) in clientTicks)
        {
            float delta = clientTickDeltas[id];
            var leftSideTick = tick.AddDuration(-delta);
            // Check if first client tick is fully inside single server tick
            if (leftSideTick.TickCount == tick.TickCount)
            {
                splitedClientTicks.AppendItemToList(tick.TickCount + 1, (id, tick, delta));
                continue;
            }

            // Add first part of tick
            float firstTickDelta = tick.TickInterval - leftSideTick.TickDuration;
            splitedClientTicks.AppendItemToList(leftSideTick.TickCount + 1,
                (id, new SoftTick(tick.TickRate) { TickCount = leftSideTick.TickCount + 1 }, firstTickDelta));

            // Add remaining parts
            float durationRemnants = delta - firstTickDelta;
            uint currentServerTick = leftSideTick.TickCount + 1;
            while (true)
            {
                if (durationRemnants > tick.TickInterval)
                {
                    currentServerTick++;
                    splitedClientTicks.AppendItemToList(currentServerTick,
                        (id, new SoftTick(tick.TickRate) { TickCount = currentServerTick }, tick.TickInterval));
                    durationRemnants -= tick.TickInterval;
                    continue;
                }
                // Add last part
                splitedClientTicks.AppendItemToList(currentServerTick + 1, (id, tick, tick.TickDuration));
                break;
            }
        }
        return splitedClientTicks;
    }

    private void OnPredictionTickUpdated(GodotWrapper<SoftTick> wrapper, float tickDelta)
    {
        if (Snapshots.Count < 1)
            return;
        var currentTick = wrapper.Value;

        CheckTickRateChange(currentTick);

        // Check if next prediction tick is less then previous one(s)
        int index = PredictionTicks.FindIndex((v) => v.Value >= currentTick);
        if (index != -1)
        {
            Logger.Singleton.Log(LogLevel.Warning, "New prediction tick is less then previous one(s). Removing commands associated with newer ticks");

            // Extract all newer tick ids
            var invalidMarkedTicks = PredictionTicks
                .GetRange(index, PredictionTicks.Count - index);
            // Leave all other ones
            PredictionTicks = PredictionTicks.GetRange(0, index);
            // Remove old commands and deltas
            foreach (var markedTick in invalidMarkedTicks)
            {
                PlayerController.Singleton.RemoveCommands(markedTick.Id);
                PredictionTickDeltas.Remove(markedTick.Id);
            }
        }

        // Check if there is new latest snapshot
        var latestSnapshot = Snapshots.Max;
        if (LastPredictionSnapshot != latestSnapshot)
        {
            // Get index of last outdated tick
            index = PredictionTicks.FindLastIndex((v) => v.Value.TickCount < latestSnapshot.Tick);
            if (index != -1)
            {
                // Extract all outdated ticks
                var oldMarkedTicks = PredictionTicks.GetRange(0, index + 1);
                // Leave all relevant ticks
                PredictionTicks = PredictionTicks
                    .GetRange(index + 1, PredictionTicks.Count - (index + 1));
                // Remove old commands and deltas
                foreach (var markedTick in oldMarkedTicks)
                {
                    PlayerController.Singleton.RemoveCommands(markedTick.Id);
                    PredictionTickDeltas.Remove(markedTick.Id);
                }
            }

            if (PlayerController.Singleton.Pawn != null)
            {
                var characterState = latestSnapshot.States
                    .FirstOrDefault(s => s.EntityId == PlayerController.Singleton.Pawn.EntityId);

                ApplyPrediction(latestSnapshot.Tick, characterState, PlayerController.Singleton.Pawn, PlayerController.Singleton);
            }
        }
        LastPredictionSnapshot = latestSnapshot;

        var newTickId = GenerateTickId();
        PredictionTicks.Add(new MarkedValue<SoftTick>(newTickId, currentTick));

        // Collect player input
        var collectedCommands = PlayerController.Singleton.CollectCommands(newTickId);
        PredictionTickDeltas.Add(newTickId, tickDelta);

        // Create placeholder data to avoid unnecessary allocations
        var emptyCommands = new List<ICommand>();

        // Generating pending commands
        var pendingCommands = DivideByServerTicks(PredictionTicks, PredictionTickDeltas)
            .Where(kv => kv.Key > latestSnapshot.Tick && kv.Key < currentTick.TickCount + 1) // TODO: Can be optimized
            .Select(kv => new KeyValuePair<uint, IEnumerable<(uint id, float delta, IEnumerable<ICommand> commands)>>(
                kv.Key, kv.Value.Select(t => PlayerController.Singleton.TryGetCommands(t.id, out var commands)
                    ? (t.id, t.delta, commands) : (t.id, t.delta, emptyCommands)))) // Check if commands exists
            .Select(kv => new CommandSnapshot(kv.Key, CommandUtils.MergeCommands(kv.Value, currentTick.TickInterval)));

        // Send all unprocessed by server commands
        var recentCommands = new RecentCommandSnapshots(pendingCommands);
        Network.Singleton.SendMessage(recentCommands);

        // Simulate character
        if (PlayerController.Singleton.Pawn != null)
        {
            PlayerController.Singleton.Pawn.ReceiveCommands(collectedCommands);
            PlayerController.Singleton.Pawn.ManualPhysicsProcess(tickDelta);
        }
    }
}
