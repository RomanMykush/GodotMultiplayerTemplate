using Godot;
using System;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public record PreInitResult(bool IsSuccessful, string Message);

public interface ILevel : IInitializable
{
    public Task<PreInitResult> PreInitialize();
    public void CleanUp();
}
