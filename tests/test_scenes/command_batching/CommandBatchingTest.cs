using Godot;
using GodotMultiplayerTemplate.Shared;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GodotMultiplayerTemplate.Tests;

public partial class CommandBatchingTest : MechanicTest
{
    private const float PacketLossPercent = 0.15f;

    private const float JumpChance = 0.2f;

    private const int NumberOfExperiments = 100;

    private static int[] TestCommandBathcing(int batchSize, Dictionary<uint, ICollection<ICommand>> commandQueue)
    {
        var didCommandArrived = new Dictionary<uint, bool>(commandQueue.Count);
        for (uint i = 0; i < commandQueue.Count; i++)
            didCommandArrived[i] = false;
        var dataSize = new List<int>(commandQueue.Count);

        // Imitating command sending
        uint maxTick = commandQueue.Keys.Max();
        for (uint i = 0; i <= maxTick + batchSize - 1; i++)
        {
            var commandBatch = new List<(uint tick, ICollection<ICommand> commands)>();
            for (uint j = 0; j < batchSize; j++)
                if (commandQueue.ContainsKey(i - j))
                    commandBatch.Add((i - j, commandQueue[i - j]));

            // Get data size
            var serializedData = MemoryPackSerializer.Serialize(commandBatch);
            dataSize.Add(serializedData.Length);

            // Simulate packet loss
            if (Random.Shared.NextDouble() > PacketLossPercent)
            {
                // Flag commands that have been delivered
                foreach (var (tick, commands) in commandBatch)
                    didCommandArrived[tick] = true;
            }
        }

        var numberOfLostCommands = didCommandArrived.Count(p => !p.Value);
        GD.Print($"Percentage of lost commands: {numberOfLostCommands / (float)didCommandArrived.Count}");
        return [.. dataSize];
    }

    public override void StartTest()
    {
        // Spacing
        GD.Print();
        GD.Print();
        GD.Print();

        // Generate commands
        var commandQueue = new Dictionary<uint, ICollection<ICommand>>();
        for (uint i = 0; i < NumberOfExperiments; i++)
        {
            var commands = new List<ICommand>() { new MoveCommand(Vector2.Zero, false) };
            // Add jump command with some chance
            if (Random.Shared.NextDouble() < JumpChance)
                commands.Add(new JumpCommand());

            commandQueue.Add(i, commands);
        }

        // Simulate packet loss for different batch sizes of commands
        GD.Print($"Logs of the Command batching test:");
        GD.Print("With batch size of 1:");
        var forBatch1 = TestCommandBathcing(1, commandQueue);
        GD.Print();

        GD.Print("With batch size of 2:");
        var forBatch2 = TestCommandBathcing(2, commandQueue);
        GD.Print();

        GD.Print("With batch size of 3:");
        var forBatch3 = TestCommandBathcing(3, commandQueue);

        // Write sizes to csv file
        var sb = new StringBuilder();
        sb.AppendLine("1,2,3");
        for (int i = 0; i < forBatch3.Length; i++)
        {
            string forBatch1Element = i < forBatch1.Length ? forBatch1[i].ToString() : string.Empty;
            string forBatch2Element = i < forBatch2.Length ? forBatch2[i].ToString() : string.Empty;
            string forBatch3Element = forBatch3[i].ToString();
            sb.AppendLine($"{forBatch1Element},{forBatch2Element},{forBatch3Element}");
        }

        using var file = FileAccess.Open("res://test_result.dat", FileAccess.ModeFlags.Write);
        file.StoreString(sb.ToString());

        EmitSignal(SignalName.TestEnded, "Data was displayed in the terminal logs and written to test_result.dat file");
    }
}
