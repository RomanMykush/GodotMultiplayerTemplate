using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public record InitLevelResult(bool IsSuccessful, string Message);

public interface ILevel
{
    public Task<InitLevelResult> Initialize();
    public IEnumerable<JobObserver> StartConstruction();
    public void CleanUp();
}
