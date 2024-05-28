using Godot;
using MemoryPack;
using System;

namespace SteampunkDnD.Shared;

[MemoryPackable]
[MemoryPackUnion(0, typeof(SyncInfo))]
[MemoryPackUnion(1, typeof(Sync))]
public partial interface INetworkMessage { }

public partial interface IEventMessage : INetworkMessage { }

public partial interface IInputMessage : INetworkMessage { }

[MemoryPackable]
public partial record SyncInfo(int ServerTicksPerSecond) : IEventMessage;

[MemoryPackable]
public partial record Sync(uint ClientTime, uint ServerTick) : IEventMessage;
