using Godot;
using System;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Shared;

public interface IControlable
{
    public void ReceiveCommands(IEnumerable<ICommand> commands);
}
