using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Shared;

public static class CommandUtils
{
    public static IEnumerable<ICommand> MergeCommands(IEnumerable<(float weight, IEnumerable<ICommand> commands)> inputCommands, float totalWeight)
    {
        // Dictionaries for different methods of merging
        var accumulateOnes = new Dictionary<Type, List<(float weight, ICommand command)>>();
        var lastOnes = new Dictionary<Type, ICommand>();
        var allOnes = new List<ICommand>();

        // Iterate over commands types
        foreach (var (weight, commands) in inputCommands)
        {
            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case LookAtCommand: // Set last one
                        lastOnes[typeof(LookAtCommand)] = cmd;
                        break;
                    case MoveCommand: // Accumulate
                        accumulateOnes.AppendItemToList(typeof(MoveCommand), (weight, cmd));
                        break;
                    case JumpCommand: // Set last one
                        lastOnes[typeof(JumpCommand)] = cmd;
                        break;
                    case AttackCommand: // Accumulate
                        accumulateOnes.AppendItemToList(typeof(AttackCommand), (weight, cmd));
                        break;
                    case InteractWithCommand: // Pass all
                        allOnes.Add(cmd);
                        break;
                    default:
                        throw new NotImplementedException($"Merging of {cmd.GetType()} command type was not implemented");
                }
            }
        }
        var mergedCommands = new List<ICommand>(lastOnes.Count + allOnes.Count + accumulateOnes.Count);
        mergedCommands.AddRange(lastOnes.Select(d => d.Value));
        mergedCommands.AddRange(allOnes);

        // Merge accumulated commands
        foreach (var cmdType in accumulateOnes.Keys)
        {
            var weightedCommands = accumulateOnes[cmdType];
            switch (cmdType)
            {
                case Type t when t == typeof(MoveCommand):
                    var moveCommands = weightedCommands.Select<
                        (float weight, ICommand command), (float weight, MoveCommand command)>(
                            c => (c.weight, (MoveCommand)c.command));
                    var directionSum = moveCommands
                        .Select(cmd => cmd.weight * cmd.command.Direction)
                        .Aggregate((sum, direction) => sum + direction);

                    bool justStarted = moveCommands.Any(c => c.command.JustStarted);
                    mergedCommands.Add(new MoveCommand(directionSum / totalWeight, justStarted));
                    break;
                case Type t when t == typeof(AttackCommand):
                    var attackCommands = weightedCommands.Select(c => (AttackCommand)c.command);
                    justStarted = attackCommands.Any(c => c.JustStarted);
                    mergedCommands.Add(new AttackCommand(justStarted));
                    break;
                default:
                    throw new NotImplementedException($"Accumulative merging of {cmdType} command type was not implemented");
            }
        }
        return mergedCommands;
    }
}
