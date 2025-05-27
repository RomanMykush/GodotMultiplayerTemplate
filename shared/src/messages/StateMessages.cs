using Godot;
using MemoryPack;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Shared;

[MemoryPackUnion(0, typeof(StateSnapshot))]
[MemoryPackUnion(1, typeof(DeltaStateSnapshot))]
[MemoryPackUnion(2, typeof(StateSnapshotAck))]
public partial interface INetworkMessage { }

// State records
[MemoryPackable]
public partial record StateSnapshot(uint Tick, ICollection<EntityState> States, IEnumerable<IMeta> MetaData) : INetworkMessage;

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

// Delta changes records
[MemoryPackable]
public partial record DeltaStateSnapshot(uint Tick, uint Baseline, ICollection<EntityState> NewEntities, Dictionary<uint, ICollection<EntityStatePropertyDelta>> DeltaStates, HashSet<uint> DeletedEntities, IEnumerable<IMeta> MetaData) : INetworkMessage;

[MemoryPackable]
[MemoryPackUnion(0, typeof(IntPropertyDelta))]
[MemoryPackUnion(1, typeof(FloatPropertyDelta))]
[MemoryPackUnion(2, typeof(UintPropertyDelta))]
[MemoryPackUnion(3, typeof(StringPropertyDelta))]
[MemoryPackUnion(4, typeof(Vector3PropertyDelta))]
public abstract partial record EntityStatePropertyDelta(ushort PropertyId);

[MemoryPackable]
public partial record IntPropertyDelta(ushort PropertyId, int Data) : EntityStatePropertyDelta(PropertyId);

[MemoryPackable]
public partial record FloatPropertyDelta(ushort PropertyId, float Data) : EntityStatePropertyDelta(PropertyId);

[MemoryPackable]
public partial record UintPropertyDelta(ushort PropertyId, uint Data) : EntityStatePropertyDelta(PropertyId);

[MemoryPackable]
public partial record StringPropertyDelta(ushort PropertyId, string Data) : EntityStatePropertyDelta(PropertyId);

[MemoryPackable]
public partial record Vector3PropertyDelta(ushort PropertyId, Vector3 Data) : EntityStatePropertyDelta(PropertyId);

// Specific state events
[MemoryPackable]
public partial record StateSnapshotAck(uint Tick) : INetworkMessage;
