using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Client;

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
