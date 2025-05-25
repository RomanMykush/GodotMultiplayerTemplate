using Godot;
using SteampunkDnD.Shared;

namespace SteampunkDnD.Client;

public partial class SyncPinger : Timer
{
    public override void _Ready()
    {
        Timeout += () =>
        {
            var sync = new SyncRequest((uint)Time.GetTicksMsec());
            Network.Singleton.SendMessage(sync);
        };
    }
}
