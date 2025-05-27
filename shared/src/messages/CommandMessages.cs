using Godot;
using MemoryPack;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Shared;

[MemoryPackUnion(3, typeof(RecentCommandSnapshots))]
public partial interface INetworkMessage { }

[MemoryPackable]
public partial record RecentCommandSnapshots(IEnumerable<CommandSnapshot> InputSnapshots) : INetworkMessage;

[MemoryPackable]
public partial record CommandSnapshot(uint Tick, IEnumerable<ICommand> Inputs);

[MemoryPackable]
[MemoryPackUnion(0, typeof(LookAtCommand))]
[MemoryPackUnion(1, typeof(MoveCommand))]
[MemoryPackUnion(2, typeof(JumpCommand))]
[MemoryPackUnion(3, typeof(AttackCommand))]
[MemoryPackUnion(4, typeof(InteractWithCommand))]
public partial interface ICommand { }

public abstract record ContiniousCommand(bool JustStarted) : ICommand;

[MemoryPackable]
public partial record LookAtCommand(Vector3 Target) : ICommand;

[MemoryPackable]
public partial record MoveCommand(Vector2 Direction, bool JustStarted) : ContiniousCommand(JustStarted);

[MemoryPackable]
public partial record JumpCommand : ICommand;

[MemoryPackable]
public partial record AttackCommand(bool JustStarted) : ContiniousCommand(JustStarted);

[MemoryPackable]
public partial record InteractWithCommand(uint TargetEntity) : ICommand;
