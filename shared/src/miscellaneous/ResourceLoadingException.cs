using Godot;
using System;

namespace SteampunkDnD.Shared;

public class ResourceLoadingException : Exception
{
    public ResourceLoadingException() : base("An unknown error occurred while loading the resource.") { }
    public ResourceLoadingException(string message) : base(message) { }
    public ResourceLoadingException(string message, Exception innerException) : base(message, innerException) { }
}
