using AsmResolver.DotNet;

namespace BepInEx.AssemblyPublicizer;

internal class NoopAssemblyResolver : IAssemblyResolver
{
    internal static NoopAssemblyResolver Instance { get; } = new();
    
    public AssemblyDefinition? Resolve(AssemblyDescriptor assembly)
    {
        return null;
    }

    public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition)
    {
    }

    public bool RemoveFromCache(AssemblyDescriptor descriptor)
    {
        return false;
    }

    public bool HasCached(AssemblyDescriptor descriptor)
    {
        return false;
    }

    public void ClearCache()
    {
    }
}
