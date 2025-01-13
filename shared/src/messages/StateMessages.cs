using Godot;
using MemoryPack;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

[MemoryPackUnion(0, typeof(StateSnapshot))]
public partial interface INetworkMessage { }

[MemoryPackable]
public partial record StateSnapshot(uint Tick, IEnumerable<EntityState> States, IEnumerable<IMeta> MetaData) : INetworkMessage;

[MemoryPackable]
[MemoryPackUnion(0, typeof(SpatialState))]
[MemoryPackUnion(1, typeof(CharacterState))]
[MemoryPackUnion(2, typeof(StaticState))]
public abstract partial record EntityState(uint EntityId);

[MemoryPackable]
public partial record SpatialState(uint EntityId, Vector3 Position, Vector3 Rotation) : EntityState(EntityId);

[MemoryPackable]
public partial record CharacterState(uint EntityId, string Kind, Vector3 Position, Vector3 Rotation, float ViewRotation, Vector3 Velocity) : SpatialState(EntityId, Position, Rotation);

[MemoryPackable]
public partial record StaticState(uint EntityId, string Kind, Vector3 Position, Vector3 Rotation) : SpatialState(EntityId, Position, Rotation);
