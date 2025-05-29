using Godot;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Tests;

public partial class ClientPredictionTest : MechanicTest
{
    private const float ProcessingDelta = 1f;

    private const int NumberOfExperiment = 4;

    // Stores pairs of ticks and directions
    private readonly SortedDictionary<uint, Vector3> ClientHistoryOfCommands = new()
    {
        { 0, new Vector3(1, 1, 0).Normalized() },
        { 1, new Vector3(0, 1, 0).Normalized() },
        { 2, new Vector3(0, 0, 1).Normalized() },
        { 3, new Vector3(0, 1, 1).Normalized() }
    };
    private readonly SortedDictionary<uint, Vector3> ServerHistoryOfPositions = [];

    private CharacterBody3D TestBody;

    public override void _Ready()
    {
        TestBody = GetNode<CharacterBody3D>("%TestBody");
    }

    public override void StartTest()
    {
        // Store current position
        ServerHistoryOfPositions[0] = TestBody.GlobalPosition;

        // Simulate full path of object
        for (uint i = 0; i < NumberOfExperiment; i++)
        {
            var direction = ClientHistoryOfCommands[i];
            TestBody.MoveAndCollide(direction * ProcessingDelta);
            ServerHistoryOfPositions[i + 1] = TestBody.GlobalPosition;
        }

        // Print input data
        GD.Print($"Logs of the Client side prediction test:");
        GD.Print("Position of simulated on server object:");
        foreach (var (tick, position) in ServerHistoryOfPositions)
            GD.Print($"   For tick {tick} position = {position}");
        GD.Print();

        GD.Print("Client movement commands:");
        foreach (var (tick, direction) in ClientHistoryOfCommands)
            GD.Print($"   For tick {tick} movement command direction = {direction}");
        GD.Print();

        // Imitating client prediction and server reconciliation
        for (uint knownTick = 0; knownTick < NumberOfExperiment; knownTick++)
        {
            // Setting known positon
            TestBody.GlobalPosition = ServerHistoryOfPositions[knownTick];
            GD.Print($"Performing client prediction from tick {knownTick} to {NumberOfExperiment}:");

            // Performing client prediction
            for (uint i = knownTick; i < NumberOfExperiment; i++)
            {
                var direction = ClientHistoryOfCommands[i];
                TestBody.MoveAndCollide(direction * ProcessingDelta);
                GD.Print($"   For tick {i + 1} predicted position = {TestBody.GlobalPosition}");
            }
            GD.Print();
        }

        EmitSignal(SignalName.TestEnded, "Data was displayed in the terminal logs");
    }
}
