using Godot;
using System;

namespace SteampunkDnD.Shared;

public record JobInfo(Job Job);

public record LoadingInfo(Job Job) : JobInfo(Job);

public record ClientLoadingInfo(Job Job, int ClientId) : JobInfo(Job);
