using Godot;
using System;
using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public interface IInitializable
{
    public IEnumerable<JobInfo> ConstructInitJobs();
}
