using Godot;
using System;
using System.Threading.Tasks;

namespace GodotMultiplayerTemplate.Shared;

public record PreInitResult(bool IsSuccessful, string Message);

public interface IGameMode : IInitializable
{
    public Task<PreInitResult> PreInitialize();
    public void CleanUp();
}
