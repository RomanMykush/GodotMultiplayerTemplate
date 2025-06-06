using MemoryPack;

namespace GodotMultiplayerTemplate.Shared;

[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerPossessionMeta))]
public partial interface IMeta { }

[MemoryPackable]
public partial record PlayerPossessionMeta(uint PlayerId, uint EntityId) : IMeta;
