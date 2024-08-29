using Godot;
using System;

namespace SteampunkDnD.Shared;

/// <summary> Represent a synchronized game object. </summary>
/// <remarks> All classes that implement this interface are expected to derive from <c>Node</c> or succesive type. </remarks>
public interface IEntity
{
    public uint EntityId { get; } // NOTE: Entities with ID value of 0 are considered not initialized

    public EntityState GetState();
    public void ApplyState(EntityState state);
}
