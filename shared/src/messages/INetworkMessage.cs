using Godot;
using MemoryPack;
using System;

namespace SteampunkDnD.Shared;

[MemoryPackable]
[MemoryPackUnion(0, typeof(StateSnapshot))]
[MemoryPackUnion(1, typeof(RecentCommandSnapshots))]
[MemoryPackUnion(2, typeof(SyncInfo))]
[MemoryPackUnion(3, typeof(SyncInfoRequest))]
[MemoryPackUnion(4, typeof(Sync))]
public partial interface INetworkMessage { }
