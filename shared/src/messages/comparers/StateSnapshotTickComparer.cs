using Godot;
using System;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Shared;

public class StateSnapshotTickComparer : IComparer<StateSnapshot>
{
    public int Compare(StateSnapshot x, StateSnapshot y)
        => (int)x.Tick - (int)y.Tick;
}
