using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Svrooij.PowerShell.DI;
using Svrooij.BetterGraph.Plumbing;
using Microsoft.Kiota.Abstractions.Authentication;
using Svrooij.PowerShell.DI.Logging;

namespace Svrooij.BetterGraph;

public class GraphStartup : PsStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Add correct IAuthenticationProvider here
        //services.AddTransient<IAuthenticationProvider, AnonymousAuthenticationProvider>();
        services.AddTransient<IAuthenticationProvider>(provider =>
        {
            return Commands.ConnectBgGraph.AuthenticationProvider ??
                   new AnonymousAuthenticationProvider();
        });
        services.AddKiotaHandlers();
        services.AddHttpClient<GraphClientFactory>(client =>
        {
            // Configure the HttpClient if needed, e.g., set base address, timeouts, etc.
            client.BaseAddress = new Uri("https://graph.microsoft.com/beta/");
        }).AttachKiotaHandlers();

        // Register the GraphServiceClient
        services.AddTransient(provider => provider.GetRequiredService<GraphClientFactory>().GetClient());
    }

    public override Action<PowerShellLoggerConfiguration>? ConfigurePowerShellLogging()
    {
        return builder =>
        {
            builder.DefaultLevel = Microsoft.Extensions.Logging.LogLevel.Debug; 
            builder.LogLevel.Add("System.Net.Http.HttpClient.GraphClientFactory.LogicalHandler", Microsoft.Extensions.Logging.LogLevel.Information);
            builder.LogLevel.Add("System.Net.Http.HttpClient.GraphClientFactory", Microsoft.Extensions.Logging.LogLevel.Warning);
            builder.StripNamespace = true;
            builder.IncludeCategory = true;
        };
    }
}


