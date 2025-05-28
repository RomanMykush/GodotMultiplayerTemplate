using Godot;

namespace GodotMultiplayerTemplate.Tests;

public abstract partial class MechanicTest : Node
{
    [Signal] public delegate void TestEndedEventHandler(string message);

    public virtual void StartTest() { }
}
