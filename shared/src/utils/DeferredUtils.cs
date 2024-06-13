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

    public static void CallDeferred(Action action) =>
        Callable.From(() => action()).CallDeferred();
}
