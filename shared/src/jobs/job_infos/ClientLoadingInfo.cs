using Godot;
using System;

namespace SteampunkDnD.Shared;

public partial class ClientLoadingInfo : JobInfo
{
    public readonly int ClientId;

    public ClientLoadingInfo(Job job, int clientId) : base(job) => ClientId = clientId;
}