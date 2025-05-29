using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Tests;

public partial class ClockSynchronizationTest : MechanicTest
{
    private const int InitialGap = 40;
    private const int DurationOfExperimentInTicks = 300;

    private readonly (int mean, int std)[] TestLetencyParams = [
        (30, 10),
        (100, 30),
        (150, 50)
    ];

    private int CurrentLatencyParamIndex = -1;

    private ClientTickClockImitation ClientClock;
    private ServerTickClockImitation ServerClock;
    private Timer UpdateTimer;

    private Stopwatch StartWatch = new();

    private List<(float time, uint currentTick, float preferredTick)> ClientTicksHistory = new(DurationOfExperimentInTicks);
    private List<(float time, uint currentTick)> ServerTicksHistory = new(DurationOfExperimentInTicks + 1);

    public override void _Ready()
    {
        ClientClock = GetNode<ClientTickClockImitation>("%ClientClock");
        ServerClock = GetNode<ServerTickClockImitation>("%ServerClock");

        // Simulating latency packet transfer time
        ClientClock.SendingSyncRequest += (wrapper) =>
        {
            var (mean, std) = TestLetencyParams[CurrentLatencyParamIndex];
            float latency = Math.Max(0, Random.Shared.NextGaussian(mean, std));
            int lastLatencyParamIndex = CurrentLatencyParamIndex;
            GetTree().CreateTimer((int)latency / 1000f).Timeout += () =>
            {
                if (lastLatencyParamIndex == CurrentLatencyParamIndex
                    && IsInstanceValid(ServerClock))
                    ServerClock.OnSyncReceived(wrapper.Value);
            };
        };
        ServerClock.SendingSync += (wrapper) =>
        {
            var (mean, std) = TestLetencyParams[CurrentLatencyParamIndex];
            float latency = Math.Max(0, Random.Shared.NextGaussian(mean, std));
            int lastLatencyParamIndex = CurrentLatencyParamIndex;
            GetTree().CreateTimer((int)latency / 1000f).Timeout += () =>
            {
                if (lastLatencyParamIndex == CurrentLatencyParamIndex
                    && IsInstanceValid(ClientClock))
                    ClientClock.OnSyncReceived(wrapper.Value);
            };
        };
    }

    public override void StartTest()
    {
        // Spacing
        GD.Print();
        GD.Print();
        GD.Print();
        GD.Print("Starting processing clocks...");

        float delta = 1f / Engine.PhysicsTicksPerSecond;
        UpdateTimer = new Godot.Timer()
        {
            OneShot = false,
            WaitTime = delta
        };
        UpdateTimer.Timeout += OnTimerTimeout;
        AddChild(UpdateTimer);

        StartTestForNextParams();
    }

    private void StartTestForNextParams()
    {
        // Clear previous data
        ClientClock.ClearState();
        ServerClock.ClearState();
        ClientTicksHistory.Clear();
        ServerTicksHistory.Clear();

        UpdateTimer.Stop();

        CurrentLatencyParamIndex++;

        // Simulate initial gap before connection
        for (int i = 0; i < InitialGap; i++)
            ServerClock.Update();

        StartWatch.Restart();
        UpdateTimer.Start();
    }

    private void OnTimerTimeout()
    {
        // Server processing
        ServerClock.Update();
        float currentTime = (float)StartWatch.Elapsed.TotalMilliseconds;
        ServerTicksHistory.Add((currentTime, ServerClock.CurrentTick));

        if (ServerClock.CurrentTick >= DurationOfExperimentInTicks + InitialGap)
        {
            var (mean, std) = TestLetencyParams[CurrentLatencyParamIndex];
            GD.Print($"Processed clocks for mean = {mean} and std = {std} latency params.");

            // Write results to files
            var clientLogs = new StringBuilder();
            clientLogs.AppendLine("time,currentTick,preferredTick");
            foreach (var (time, currentTick, preferredTick) in ClientTicksHistory)
                clientLogs.AppendLine($"{time},{currentTick},{preferredTick}");

            using var clientLogsFile = FileAccess.Open($"res://test_result_{CurrentLatencyParamIndex + 1}_client.dat", FileAccess.ModeFlags.Write);
            clientLogsFile.StoreString(clientLogs.ToString());

            var serverLogs = new StringBuilder();
            serverLogs.AppendLine("time,currentTick");
            foreach (var (time, currentTick) in ServerTicksHistory)
                serverLogs.AppendLine($"{time},{currentTick}");

            using var serverLogsFile = FileAccess.Open($"res://test_result_{CurrentLatencyParamIndex + 1}_server.dat", FileAccess.ModeFlags.Write);
            serverLogsFile.StoreString(serverLogs.ToString());

            // Check if all subtests were competed
            if (CurrentLatencyParamIndex >= TestLetencyParams.Length - 1)
            {
                GD.Print("Ended processing clocks.");
                EmitSignal(SignalName.TestEnded, "Data was written to test_result_x_client.dat and test_result_x_server.dat files");
                return;
            }

            StartTestForNextParams();
            return;
        }

        // Client processing
        float delta = 1f / Engine.PhysicsTicksPerSecond;
        ClientClock.Update(delta);

        ClientTicksHistory.Add((currentTime, ClientClock.CurrentTick, ClientClock.PreferredTick));
    }
}
