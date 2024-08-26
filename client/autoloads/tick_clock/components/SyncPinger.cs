using Godot;
using SteampunkDnD.Shared;
using System;

namespace SteampunkDnD.Client;

public partial class SyncPinger : Timer
{
    public override void _Ready()
    {
        Timeout += () =>
        {
            var sync = new Sync((uint)Time.GetTicksMsec(), 0);
            Network.Singleton.SendPacket(sync);
        };
    }
}
