using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Svrooij.PowerShell.DI;
using System.Management.Automation;

namespace Svrooij.BetterGraph.Commands;
/// <summary>
/// <para type="synopsis">Connect to Microsoft Graph</para>
/// <para type="description">As with the regular module, you'll need to connect to Graph.</para>
/// </summary>
/// <psOrder>2</psOrder>
/// <parameterSet>
/// <para type="name">Interactive</para>
/// <para type="description">Interactive browser login. This will integrate with the native broker based login screen on Windows and with the default browser on other platforms.</para>
/// </parameterSet>
/// <parameterSet>
/// <para type="name">UseDefaultCredentials</para>
/// <para type="description">A more extended version of the Managed Identity is the Default Credentials, this will use the [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet&amp;wt.mc_id=SEC-MVP-5004985), from the `Azure.Identity` package. This will try several methods to authenticate, Environment Variables, Managed Identity, Azure CLI and more.</para>
/// </parameterSet>
/// <parameterSet>
/// <para type="name">Token</para>
/// <para type="description">Let's say you have a token from another source, just hand us to token and we'll use it to connect to Intune. This token has a limited lifetime, so you'll be responsible for refreshing it.</para>
/// </parameterSet>
/// <parameterSet>
/// <para type="name">ClientCredentials</para>
/// <para type="description">:::warning Last resort\r\nUsing client credentials is not recommended because you'll have to keep the secret, **secret**!\r\n\r\nPlease let us know if you have to use this method, we might be able to help you with a better solution.\r\n:::</para>
/// </parameterSet>
[Cmdlet(VerbsCommunications.Connect, "BgGraph", DefaultParameterSetName = ParamSetInteractive)]
public class ConnectBgGraph : DependencyCmdlet<Startup>
{
    private const string ParamSetInteractive = "Interactive";
    private const string ParamSetClientCredentials = "ClientCredentials";
    private const string DefaultClientCredentialScope = "https://graph.microsoft.com/.default";

    /// <summary>
    /// 
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = ParamSetInteractive,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Use a username to trigger interactive login or SSO")]
    public string? Username { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter(
        Mandatory = false,
        Position = 2,
        ParameterSetName = ParamSetInteractive,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Specify the tenant ID, optional. Loaded from `AZURE_TENANT_ID`")]
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = ParamSetClientCredentials,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Specify the tenant ID. Loaded from `AZURE_TENANT_ID`")]
    public string? TenantId { get; set; } = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

    /// <summary>
    /// 
    /// </summary>
    [Parameter(
        Mandatory = false,
        Position = 2,
        ParameterSetName = ParamSetInteractive,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Disable Windows authentication broker")]
    public SwitchParameter NoBroker { get; set; }

    [Parameter(
        Mandatory = false,
        Position = 3,
        ParameterSetName = ParamSetInteractive,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Specify the alternative client ID, optional. Loaded from `AZURE_CLIENT_ID`")]
    [Parameter(
        Mandatory = true,
        Position = 2,
        ParameterSetName = ParamSetClientCredentials,
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Specify the client ID. Loaded from `AZURE_CLIENT_ID`")]
    public string? ClientId { get; set; } = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

    /// <summary>
    /// Client secret for client credentials flow
    /// </summary>
    [Parameter(
               Mandatory = true,
               Position = 3,
               ParameterSetName = ParamSetClientCredentials,
               ValueFromPipeline = false,
               ValueFromPipelineByPropertyName = false,
               HelpMessage = "Specify the client secret. Loaded from `AZURE_CLIENT_SECRET`")]
    public string? ClientSecret { get; set; } = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

    /// <summary>
    /// One or more scopes to request
    /// </summary>
    [Parameter(
    Mandatory = true,
    Position = 10,
    ParameterSetName = ParamSetInteractive,
    ValueFromPipeline = false,
    ValueFromPipelineByPropertyName = false,
    HelpMessage = "Specify the scopes to request")]
    [Parameter(
    Mandatory = false,
    Position = 10,
    ParameterSetName = ParamSetClientCredentials,
    ValueFromPipeline = false,
    ValueFromPipelineByPropertyName = false,
    HelpMessage = "Specify the scopes to request, default is `https://graph.microsoft.com/.default`")]
    public string[]? Scopes { get; set; } = Environment.GetEnvironmentVariable("AZURE_SCOPES")?.Split(' ');

    /// <summary>
    /// Use a token from another source to connect to Intune
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = nameof(Token),
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Use a token from another source to connect to Intune, this is the least preferred way to use")]
    public string? Token { get; set; } = Environment.GetEnvironmentVariable("AZURE_TOKEN");

    /// <summary>
    /// Use default Azure Credentials from Azure.Identity to connect to Graph
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = nameof(UseDefaultCredentials),
        ValueFromPipeline = false,
        ValueFromPipelineByPropertyName = false,
        HelpMessage = "Use default Azure Credentials from Azure.Identity to connect to Intune")]
    public SwitchParameter UseDefaultCredentials { get; set; } = Environment.GetEnvironmentVariable("AZURE_USE_DEFAULT_CREDENTIALS")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;


    [ServiceDependency]
    private ILogger<ConnectBgGraph>? logger;

    /// <inheritdoc />
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (this.ParameterSetName == nameof(Token) && !string.IsNullOrWhiteSpace(this.Token))
        {
            logger?.LogInformation("Using static token for authentication");
            AuthenticationProvider = new Plumbing.StaticAuthenticationProvider(this.Token);
        }
        else if (this.ParameterSetName == ParamSetInteractive)
        {
            logger?.LogInformation("Using interactive authentication for connecting to Microsoft Graph");
            AuthenticationProvider = new Plumbing.InteractiveAuthenticationProvider(new Plumbing.InteractiveAuthenticationProviderOptions
            {
                Username = this.Username,
                TenantId = this.TenantId,
                ClientId = this.ClientId,
                Scopes = this.Scopes!
            });
        }
        else if (this.UseDefaultCredentials)
        {
            logger?.LogInformation("Using default Azure credentials for connecting to Microsoft Graph");
            TokenCredential credentials = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
            {
                ExcludeSharedTokenCacheCredential = false, // Avoid using the shared token cache to prevent conflicts with other applications
            });
            var scopes = this.Scopes ?? new[] { DefaultClientCredentialScope };
            AuthenticationProvider = new Microsoft.Graph.Authentication.AzureIdentityAuthenticationProvider(credentials, null, null, isCaeEnabled: false, scopes: scopes);
        }
        else if (ParameterSetName == ParamSetClientCredentials)
        {
            if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(TenantId) &&
                !string.IsNullOrEmpty(ClientSecret))
            {
                var scopes = this.Scopes ?? new[] { DefaultClientCredentialScope };
                AuthenticationProvider = new Microsoft.Graph.Authentication.AzureIdentityAuthenticationProvider(
                    new Azure.Identity.ClientSecretCredential(TenantId, ClientId, ClientSecret,
                        new Azure.Identity.ClientSecretCredentialOptions
                        {
                            TokenCachePersistenceOptions = new Azure.Identity.TokenCachePersistenceOptions
                            {
                                Name = "BetterGraph-PowerShell-CC",
                                UnsafeAllowUnencryptedStorage = true,
                            }
                        }), isCaeEnabled: false, scopes: scopes);
            }
            else
            {
                throw new ArgumentException("Not all parameters for client credentials are specified",
                    nameof(ParamSetClientCredentials));
            }
        }
        else
        {
            throw new ArgumentException("Invalid parameter set or parameters provided.");
        }
    }

    internal static IAuthenticationProvider? AuthenticationProvider { get; private set; } = null;

    internal static void ResetAuthenticationProvider()
    {
        AuthenticationProvider = null;
    }
}
