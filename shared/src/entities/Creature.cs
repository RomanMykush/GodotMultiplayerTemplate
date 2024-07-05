using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteampunkDnD.Shared;

public partial class Creature : CharacterBody3D, ISpatial, IControlable
{
    public uint EntityId { get; set; }
    public string Specie { get; set; } // TODO: Add updates to model, skeleton rig and animations when this property changes
    private IEnumerable<ICommand> LastInputs;

    public void ReceiveCommands(IEnumerable<ICommand> commands) => LastInputs = commands;

    public override void _PhysicsProcess(double delta)
    {
        // TODO: Add physics and command processing

        // Get all continuous inputs and mark them as not started recenty
        LastInputs = LastInputs.OfType<ContiniousCommand>()
            .Select(i => i with { JustStarted = false });
    }
}
