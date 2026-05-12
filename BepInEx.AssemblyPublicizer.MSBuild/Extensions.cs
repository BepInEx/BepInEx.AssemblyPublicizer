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
        if (!taskItem.HasMetadata(metadataName))
        {
            metadata = null;
            return false;
        }

        // Fix a problem where when one tries to publicize PhotonRealtime, PhotonUnityNetworking,
        // and PhotonUnityNetworking.Utilities from the R.E.P.O.GameLibs.Steam nuget package.
        // Why is this needed? Because there are mods such as LateJoin that needs access to
        // internal fields/methods to these Proton assemblies and avoiding reflection helps solve
        // 100% of problems related to modding R.E.P.O. while not sacrificing performance.
        // Also the fact that Reflection sucks and way too many bugs can happen with it.
        // Problem seems to have existed from 0.4.1 according to git blame.
        metadata = taskItem.GetMetadata(metadataName);
        if (!string.IsNullOrWhiteSpace(metadata))
        {
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
