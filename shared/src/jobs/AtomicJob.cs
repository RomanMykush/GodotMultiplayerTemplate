using Godot;
using System;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Shared;

public partial class AtomicJob : Job
{
    private readonly Func<Task> JobFunc;

    public AtomicJob(Func<Task> jobFunc)
    {
        JobFunc = jobFunc;
    }

    public async override Task Run()
    {
        try
        {
            await JobFunc();
        }
        catch (Exception)
        {
            DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
            throw;
        }

        DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Completed));
    }
}
