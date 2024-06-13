using Godot;
using System;

namespace SteampunkDnD.Shared;

public partial class JobInfo
{
    // TODO: Refactor it to required
    public readonly Job Job;

    public JobInfo(Job job) => Job = job;
}
