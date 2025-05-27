using Godot;
using System;

namespace GodotMultiplayerTemplate.Shared;

public partial class StaticProp : StaticBody3D, ISpatial
{
    [Export] public string Kind { get; private set; }

    public uint EntityId { get; private set; }

    public EntityState GetState() => new StaticState(EntityId, Kind, Position, Rotation);

    public void ApplyState(EntityState state)
    {
        if (state is not StaticState staticState)
            throw new ArgumentException("Invalid argument type was passed");

        if (EntityId == 0)
            EntityId = staticState.EntityId;
        else if (EntityId != staticState.EntityId)
            throw new ArgumentException("State with wrong Id was passed");

        if (Kind != staticState.Kind)
            Logger.Singleton.Log(LogLevel.Error, "State with wrong Kind was passed");

        Position = staticState.Position;
        Rotation = staticState.Rotation;
    }
}
