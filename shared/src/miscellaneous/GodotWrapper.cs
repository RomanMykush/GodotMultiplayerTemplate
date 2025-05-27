using Godot;
using System;

namespace GodotMultiplayerTemplate.Shared;

public partial class GodotWrapper<T> : GodotObject
{
    public T Value { get; set; }
    public GodotWrapper(T value) => Value = value;
}
