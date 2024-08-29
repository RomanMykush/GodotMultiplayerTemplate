using Godot;
using System;
using System.Text;

namespace SteampunkDnD.Shared;

public static class StringUtils
{
    public static string CamelToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(input[0]));
        for (int i = 1; i < input.Length; ++i)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }

        return sb.ToString();
    }
}