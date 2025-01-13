using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Shared;

public static class CommandUtils
{
    public static IEnumerable<ICommand> MergeCommands(IEnumerable<(uint priority, float weight, IEnumerable<ICommand> commands)> inputCommands, float totalWeight)
    {
        // Dictionaries for different methods of merging
        var bestOnes = new Dictionary<Type, (uint priority, ICommand command)>();
        var accumulateOnes = new Dictionary<Type, List<(float weight, ICommand command)>>();
        var anyOnes = new Dictionary<Type, ICommand>();
        var allOnes = new List<ICommand>();

        // Iterate over commands types
        foreach (var (priority, weight, commands) in inputCommands)
        {
            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case LookAtCommand: // Find most important one
                        if (bestOnes.ContainsKey(typeof(LookAtCommand)))
                        {
                            var previous = bestOnes[typeof(LookAtCommand)];
                            if (previous.priority > priority)
                                break;
                        }
                        bestOnes[typeof(LookAtCommand)] = (priority, cmd);
                        break;
                    case MoveCommand: // Accumulate
                        accumulateOnes.AppendItemToList(typeof(MoveCommand), (weight, cmd));
                        break;
                    case JumpCommand: // Set any
                        anyOnes[typeof(JumpCommand)] = cmd;
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
        var mergedCommands = new List<ICommand>();
        mergedCommands.AddRange(bestOnes.Select(d => d.Value.command));
        mergedCommands.AddRange(anyOnes.Select(d => d.Value));
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
