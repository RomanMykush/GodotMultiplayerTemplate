using Godot;
using MemoryPack;
using System;

namespace SteampunkDnD.Shared;

[MemoryPackUnion(2, typeof(SyncInfo))]
[MemoryPackUnion(3, typeof(SyncInfoRequest))]
[MemoryPackUnion(4, typeof(Sync))]
public partial interface INetworkMessage { }

public abstract record NetworkEvent : INetworkMessage;

[MemoryPackable]
public partial record SyncInfo(int ServerTicksPerSecond) : NetworkEvent;

[MemoryPackable]
public partial record SyncInfoRequest() : NetworkEvent;

[MemoryPackable]
public partial record Sync(uint ClientTime, uint ServerTick) : NetworkEvent;
