using System.Management.Automation;
using System.Reflection;

namespace Svrooij.BetterGraph.Plumbing;
/// <summary>
/// Custom module initializer for importing required assemblies at runtime.
/// </summary>
/// <remarks>This is to fix the mess with powershell assembly loading. This class will be called automatically when this module is imported</remarks>
public class ModuleInitializer : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    private static readonly string[] ModulesToImport = new[]
    {
        "Azure.Core",
        "Azure.Identity",
        "Microsoft.Extensions.Configuration.Abstractions",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Http",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Logging",
        "Microsoft.Extensions.Logging.Configuration",
        "Microsoft.Kiota.Abstractions",
        "Microsoft.Kiota.Authentication.Azure",
        "Microsoft.Kiota.Http.HttpClientLibrary",
        "Microsoft.Graph.Core",
        "Microsoft.Graph.Beta",
        "Microsoft.Identity.Client",
        "Microsoft.Identity.Client.Broker"
    };

    private ModuleLoadContext? loadContext;
    /// <inheritdoc />
    public void OnImport()
    {
        string moduleDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        loadContext = new ModuleLoadContext(moduleDirectory);

        foreach (var module in ModulesToImport)
        {
            string assemblyPath = Path.Combine(moduleDirectory, $"{module}.dll");
            if (File.Exists(assemblyPath))
            {
                loadContext.LoadFromAssemblyPath(assemblyPath);
            }
            else
            {
                Console.Error.WriteLine($"Warning: Assembly '{module}' not found at '{assemblyPath}'. Ensure it is present in the module directory.");
            }
        }
    }

    /// <inheritdoc />
    public void OnRemove(PSModuleInfo psModuleInfo)
    {
        loadContext?.Unload();
    }
}