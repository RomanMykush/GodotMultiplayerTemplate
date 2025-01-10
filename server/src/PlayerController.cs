using SteampunkDnD.Shared;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Server;

public partial class PlayerController : CharacterController
{
    // TODO: Make property required after migration to .NET 8
    public uint PlayerId;
    private Dictionary<uint, IEnumerable<ICommand>> PendingCommands = new();

    public override void _Ready()
    {
        Network.Singleton.MessageReceived += (peer, msg) =>
        {
            if (AuthService.Singleton.GetPlayerId(peer) != PlayerId)
                return;

            if (msg.Value is RecentCommandSnapshots commandSnapshots)
                OnCommandSnapshotsReceived(commandSnapshots);
        };
    }

    public void OnCommandSnapshotsReceived(RecentCommandSnapshots commandSnapshots)
    {
        // TODO: Add commands validation
        foreach (var (tick, commands) in commandSnapshots.InputSnapshots)
            PendingCommands[tick] = commands;
    }

    public override void ApplyCommands(uint currentTick)
    {
        if (PendingCommands.TryGetValue(currentTick, out IEnumerable<ICommand> commands))
            Pawn.ReceiveCommands(commands);

        // Clean up old commands
        PendingCommands = PendingCommands.Where(pair => pair.Key > currentTick)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
