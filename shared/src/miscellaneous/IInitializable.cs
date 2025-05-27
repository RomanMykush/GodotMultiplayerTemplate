using Godot;
using System;
using System.Collections.Generic;

namespace GodotMultiplayerTemplate.Shared;

public interface IInitializable
{
    public IEnumerable<JobInfo> ConstructInitJobs();
}
