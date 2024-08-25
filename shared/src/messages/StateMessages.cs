using Godot;
using MemoryPack;
using System;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

[MemoryPackable]
public partial record StateSnapshot(uint Tick, IEnumerable<EntityState> States) : INetworkMessage;

[MemoryPackable]
[MemoryPackUnion(0, typeof(SpatialState))]
[MemoryPackUnion(1, typeof(CharacterState))]
public abstract partial record EntityState(uint EntityId);

[MemoryPackable]
public partial record SpatialState(uint EntityId, Vector3 Position, Vector3 Rotation) : EntityState(EntityId);

[MemoryPackable]
public partial record CharacterState(uint EntityId, string Specie, Vector3 Position, Vector3 Rotation, Vector3 Velocity) : SpatialState(EntityId, Position, Rotation);
