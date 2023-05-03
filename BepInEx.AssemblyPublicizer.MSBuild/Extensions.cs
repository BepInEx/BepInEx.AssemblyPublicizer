#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;

namespace BepInEx.AssemblyPublicizer.MSBuild;

internal static class Extensions
{
    public static bool HasMetadata(this ITaskItem taskItem, string metadataName)
    {
        var metadataNames = (ICollection<string>)taskItem.MetadataNames;
        return metadataNames.Contains(metadataName);
    }

    public static bool TryGetMetadata(this ITaskItem taskItem, string metadataName, [NotNullWhen(true)] out string? metadata)
    {
        if (taskItem.HasMetadata(metadataName))
        {
            metadata = taskItem.GetMetadata(metadataName);
            return true;
        }

        metadata = null;
        return false;
    }

    public static bool GetBoolMetadata(this ITaskItem taskItem, string metadataName)
    {
        return taskItem.GetMetadata(metadataName).Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
