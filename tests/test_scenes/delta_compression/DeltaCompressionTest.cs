using Godot;
using GodotMultiplayerTemplate.Shared;
using MemoryPack;

namespace GodotMultiplayerTemplate.Tests;

public partial class DeltaCompressionTest : MechanicTest
{
    private static readonly StateSnapshot Baseline = new(1, [
        new CharacterState(1, "Elf", new Vector3(1, 2, 3), new Vector3(3, 3, 3), 2, Vector3.Zero),
        new CharacterState(2, "Dwarf", new Vector3(2, 3, 4), new Vector3(2, 2, 2), 3, Vector3.Zero),
        new StaticState(3, "Wall", new Vector3(5, 5, 1), new Vector3(5, 5, 1)),
        new StaticState(4, "Floor", Vector3.Zero, Vector3.Zero),
        new StaticState(5, "Chair", new Vector3(1, 2, 3), new Vector3(5, 3, 3)),
    ], []);

    private static readonly StateSnapshot ToBeEncoded = new(2, [
        new CharacterState(1, "Elf", new Vector3(4, 5, 6), new Vector3(3, 3, 3), 2, new Vector3(3, 3, 3)),
        new CharacterState(2, "Gnome", new Vector3(2, 3, 4), new Vector3(2, 2, 2), 1, Vector3.Zero),
        new StaticState(4, "Floor", Vector3.Zero, Vector3.Zero),
        new StaticState(5, "Chair", new Vector3(1, 1, 1), new Vector3(3, 1, 1)),
        new StaticState(6, "Lamp", new Vector3(2, 3, 2), new Vector3(3, 3, 5)),
    ], []);

    public override void StartTest()
    {
        // Spacing
        GD.Print();
        GD.Print();
        GD.Print();

        // Input data
        GD.Print($"Logs of the Delta compression test:");
        GD.Print();
        GD.Print($"Input data:");

        GD.Print($"Snapshot #{Baseline.Tick}:");
        foreach (var state in Baseline.States)
            GD.Print($"   {state}");

        GD.Print($"Snapshot #{ToBeEncoded.Tick}:");
        foreach (var state in ToBeEncoded.States)
            GD.Print($"   {state}");

        GD.Print();
        GD.Print($"Delta compressed snapshot #{ToBeEncoded.Tick}:");

        // Encode test
        var delta = StateSnapshotUtils.DeltaEncode(Baseline, ToBeEncoded);

        GD.Print($"New states:");
        foreach (var state in delta.NewEntities)
            GD.Print($"   {state}");

        GD.Print($"Removed states: [{string.Join(", ", delta.DeletedEntities)}]");

        GD.Print($"Delta changes of properties of modified states:");
        foreach (var (key, deltas) in delta.DeltaStates)
        {
            GD.Print($"   For state #{key}:");
            foreach (var item in deltas)
                GD.Print($"      {item}");
        }

        // Decode test
        var decodedSnapshot = StateSnapshotUtils.DeltaDecode(Baseline, delta);

        GD.Print();
        GD.Print($"Decoded snapshot #{decodedSnapshot.Tick}:");
        foreach (var state in decodedSnapshot.States)
            GD.Print($"   {state}");

        // Size improvement
        var input2Data = MemoryPackSerializer.Serialize(ToBeEncoded);
        var deltaData = MemoryPackSerializer.Serialize(delta);

        GD.Print();
        GD.Print("Difference in sizes:");
        GD.Print($"Original size: {input2Data.Length}");
        GD.Print($"Delta compressed size: {deltaData.Length}");

        EmitSignal(SignalName.TestEnded, "Data was displayed in the terminal logs");
    }
}
