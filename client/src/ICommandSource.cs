using System.Collections.Generic;
using SteampunkDnD.Shared;

namespace SteampunkDnD.Client;

public interface ICommandSource
{
    public Character Pawn { get; set; }

    public ICollection<ICommand> CollectCommands();
}
