using System.Collections.Generic;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Client;

public interface ICommandSource
{
    public Character Pawn { get; set; }

    public ICollection<ICommand> CollectCommands();
}
