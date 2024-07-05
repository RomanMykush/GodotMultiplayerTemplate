using Godot;
using MemoryPack;
using System;

namespace SteampunkDnD.Shared;

public abstract record NetworkEvent : INetworkMessage;

[MemoryPackable]
public partial record SyncInfo(int ServerTicksPerSecond) : NetworkEvent;

[MemoryPackable]
public partial record SyncInfoRequest() : NetworkEvent;

[MemoryPackable]
public partial record Sync(uint ClientTime, uint ServerTick) : NetworkEvent;
