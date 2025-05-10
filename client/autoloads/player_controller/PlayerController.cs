using Godot;
using SteampunkDnD.Shared;
using System.Collections.Generic;

namespace SteampunkDnD.Client;

public partial class PlayerController : Node
{
    public static PlayerController Singleton { get; private set; }

    private readonly List<ICommandSource> CommandSources = [];

    private Character _pawn;
    public Character Pawn
    {
        get => _pawn;
        set
        {
            // Avoid unneceserry signals resubscriptions
            if (_pawn == value)
                return;

            _pawn = value;
            foreach (var cmdSource in CommandSources)
                cmdSource.Pawn = _pawn;
        }
    }
    /// <summary>
    /// Dictionary of <c>SoftTick</c> ids with corresponding <c>Command</c>s.
    /// </summary>
    private readonly Dictionary<uint, IEnumerable<ICommand>> BufferedCommands = [];

    public override void _Ready()
    {
        Singleton = this;

        CommandSources.Add(GetNode<CameraContainer>("%Camera"));
        CommandSources.Add(GetNode<KeyInputTranslator>("%KeyInput"));
    }

    /// <summary>
    /// Collects <c>Command</c>s, saves them to internal <c>Dictionary</c> and returns their copy.
    /// </summary>
    public IEnumerable<ICommand> CollectCommands(uint tickId)
    {
        if (!IsInstanceValid(Pawn))
            return null;

        // Collect commands
        var commands = new List<ICommand>();
        foreach (var cmdSource in CommandSources)
            commands.AddRange(cmdSource.CollectCommands());

        // Store commands
        BufferedCommands.Add(tickId, commands);

        return commands;
    }

    public bool TryGetCommands(uint tick, out IEnumerable<ICommand> result) =>
        BufferedCommands.TryGetValue(tick, out result);

    public bool RemoveCommands(uint tick) =>
        BufferedCommands.Remove(tick);
}
