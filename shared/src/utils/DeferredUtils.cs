using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public static class DeferredUtils
{
    public static async Task<T> RunDeferred<T>(Func<T> func)
    {
        var semaphore = new SemaphoreSlim(0, 1);
        T result = default;
        // Request deferred task
        Callable.From(() =>
        {
            result = func();
            semaphore.Release();
        }).CallDeferred();

        // Wait for deferred task to finish
        await semaphore.WaitAsync();
        return result;
    }

    public static void CallDeferred(Action action) =>
        Callable.From(() => action()).CallDeferred();
}
