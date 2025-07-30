using System.Reflection;
using System.Runtime.Loader;

namespace Svrooij.BetterGraph.Plumbing;

internal class ModuleLoadContext : AssemblyLoadContext
{
    private string _modulePath;

    public ModuleLoadContext(string modulePath) : base(isCollectible: false)
    {
        _modulePath = modulePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string assemblyPath = Path.Combine(_modulePath, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        return null;
    }
}