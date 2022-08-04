using System;

namespace BepInEx.AssemblyPublicizer;

public class AssemblyPublicizerOptions
{
    public PublicizeTarget Target { get; set; } = PublicizeTarget.All;
    public bool PublicizeCompilerGenerated { get; set; } = false;
    public bool IncludeOriginalAttributesAttribute { get; set; } = true;

    public bool Strip { get; set; } = false;

    internal bool HasTarget(PublicizeTarget target)
    {
        return (Target & target) != 0;
    }
}

[Flags]
public enum PublicizeTarget
{
    All = Types | Methods | Fields,
    None = 0,
    Types = 1 << 0,
    Methods = 1 << 1,
    Fields = 1 << 2,
}
