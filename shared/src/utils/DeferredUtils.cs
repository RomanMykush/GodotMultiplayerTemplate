using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Shared;

public static class DeferredUtils
{
    /// <summary> Queues deferred call, result of which can be awaited. Passed <c>Func</c> will be executed during idle frame on main thread. </summary>
    public static async Task<T> RunDeferred<T>(Func<T> func)
    {
        var semaphore = new SemaphoreSlim(0, 1);
        T result = default;
        // Request deferred task
        Callable.From(() =>
        {
            try
            {
                result = func();
            }
            finally
            {
                semaphore.Release();
            }
        }).CallDeferred();

        // Wait for deferred task to finish
        await semaphore.WaitAsync();
        return result;
    }

    /// <summary> Queues deferred call, which can be awaited. Passed <c>Action</c> will be executed during idle frame on main thread. </summary>
    public static async Task RunDeferred(Action action)
    {
        var semaphore = new SemaphoreSlim(0, 1);
        // Request deferred task
        Callable.From(() =>
        {
            try
            {
                action();
            }
            finally
            {
                semaphore.Release();
            }
        }).CallDeferred();

        // Wait for deferred task to finish
        await semaphore.WaitAsync();
    }

    /// <summary> Queues deferred call of <c>Action</c>, which will be executed during idle frame on main thread. </summary>
    public static void CallDeferred(Action action) =>
        Callable.From(() => action()).CallDeferred();
}
