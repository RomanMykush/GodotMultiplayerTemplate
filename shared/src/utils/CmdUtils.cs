using Godot;
using System;
using System.Linq;

namespace GodotMultiplayerTemplate.Shared;

public static class CmdUtils
{
    private static readonly Lazy<string[]> _cmdArguments = new(OS.GetCmdlineUserArgs());
    public static string[] CmdArguments => _cmdArguments.Value;

    /// <summary> Check if one of passed cmd flags matches. </summary>
    /// <remarks> Example of flag <c>-t</c> or <c>--test</c>. </remarks>
    public static bool CheckFlag(string flag) =>
        CmdArguments.Contains(flag);

    /// <summary> Returns value of passed cmd parameter. </summary>
    /// <remarks> Example of parameter: <c>-p 2000</c> or <c>--port 2000</c>. </remarks>
    public static bool GetParameterValue<T>(string arg, out T value) where T : struct
    {
        // Check if argument was passed
        int index = Array.FindIndex(CmdArguments, a => a == arg);
        if (index == -1)
        {
            value = default;
            return false;
        }

        // Check if sufficient number of arguemnts was passed
        if (index + 1 >= CmdArguments.Length)
            throw new ArgumentException($"Value after '{arg}' argument is missing");

        // Check if next argument is value
        try
        {
            value = (T)Convert.ChangeType(CmdArguments[index + 1], typeof(T));
        }
        catch (System.Exception)
        {
            throw new ArgumentException($"Value after '{arg}' argument is missing");
        }

        return true;
    }
}
