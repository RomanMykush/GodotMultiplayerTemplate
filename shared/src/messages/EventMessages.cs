using Godot;
using MemoryPack;
using System;

namespace SteampunkDnD.Shared;

[MemoryPackUnion(2, typeof(SyncInfo))]
[MemoryPackUnion(3, typeof(SyncInfoRequest))]
[MemoryPackUnion(4, typeof(Sync))]
[MemoryPackUnion(5, typeof(ServerAuth))]
[MemoryPackUnion(6, typeof(ClientAuth))]
[MemoryPackUnion(7, typeof(NewPlayerIdRequest))]
[MemoryPackUnion(8, typeof(NewPlayerId))]
public partial interface INetworkMessage { }

public abstract record NetworkEvent : INetworkMessage;

[MemoryPackable]
public partial record SyncInfo(int ServerTicksPerSecond) : NetworkEvent;

[MemoryPackable]
public partial record SyncInfoRequest() : NetworkEvent;

[MemoryPackable]
public partial record Sync(uint ClientTime, uint ServerTick) : NetworkEvent;

[MemoryPackable]
public partial record ServerAuth(uint ServerId) : NetworkEvent;

[MemoryPackable]
public partial record ClientAuth(uint PlayerId) : NetworkEvent;

[MemoryPackable]
public partial record NewPlayerIdRequest() : NetworkEvent;

[MemoryPackable]
public partial record NewPlayerId(uint PlayerId) : NetworkEvent;
