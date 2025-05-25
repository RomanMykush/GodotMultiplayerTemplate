using MemoryPack;

namespace SteampunkDnD.Shared;

[MemoryPackUnion(4, typeof(SyncInfo))]
[MemoryPackUnion(5, typeof(SyncInfoRequest))]
[MemoryPackUnion(6, typeof(Sync))]
[MemoryPackUnion(7, typeof(SyncRequest))]
[MemoryPackUnion(8, typeof(ServerAuth))]
[MemoryPackUnion(9, typeof(ClientAuth))]
[MemoryPackUnion(10, typeof(NewPlayerIdRequest))]
[MemoryPackUnion(11, typeof(NewPlayerId))]
public partial interface INetworkMessage { }

public abstract record NetworkEvent : INetworkMessage;

[MemoryPackable]
public partial record SyncInfo(int ServerTicksPerSecond) : NetworkEvent;

[MemoryPackable]
public partial record SyncInfoRequest() : NetworkEvent;

[MemoryPackable]
public partial record Sync(uint ClientTime, uint ServerTick, float ServerTickDuration) : NetworkEvent;

[MemoryPackable]
public partial record SyncRequest(uint ClientTime) : NetworkEvent;

[MemoryPackable]
public partial record ServerAuth(uint ServerId) : NetworkEvent;

[MemoryPackable]
public partial record ClientAuth(uint PlayerId) : NetworkEvent;

[MemoryPackable]
public partial record NewPlayerIdRequest() : NetworkEvent;

[MemoryPackable]
public partial record NewPlayerId(uint PlayerId) : NetworkEvent;
