using Godot;
using MemoryPack;
using System;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

[MemoryPackable]
public partial record StateSnapshot(uint Tick, IEnumerable<EntityState> States) : INetworkMessage;

[MemoryPackable]
[MemoryPackUnion(0, typeof(SpatialState))]
[MemoryPackUnion(1, typeof(CreatureState))]
public abstract partial record EntityState(uint EntityId);

[MemoryPackable]
public partial record SpatialState(uint EntityId, Vector3 Position, Quaternion Rotation) : EntityState(EntityId);

[MemoryPackable]
public partial record CreatureState(uint EntityId, Vector3 Position, Quaternion Rotation, string Specie) : SpatialState(EntityId, Position, Rotation);
