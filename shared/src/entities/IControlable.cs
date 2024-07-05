using Godot;
using System;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public interface IControlable
{
    public void ReceiveCommands(IEnumerable<ICommand> commands);
}
