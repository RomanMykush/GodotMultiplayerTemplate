using Godot;
using System;

namespace GodotMultiplayerTemplate.Shared;

public interface ISpatial : IEntity
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}
