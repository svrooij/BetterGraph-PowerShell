using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;

namespace Svrooij.BetterGraph.Plumbing;

/// <summary>
/// Provides an authentication provider that supports interactive user authentication using the Microsoft Authentication
/// Library (MSAL).
/// </summary>
/// <remarks>This provider is designed to acquire tokens interactively or silently for accessing resources.
/// It supports caching of authentication tokens and can be configured to use a broker for
/// authentication on supported platforms.</remarks>
internal sealed class InteractiveAuthenticationProvider : IAuthenticationProvider
{
    private readonly InteractiveAuthenticationProviderOptions _options;
    private readonly IPublicClientApplication publicClientApplication;
    private bool CacheLoaded = false;
    private const string DefaultClientId = "6fb61555-6571-4835-8f61-23bcce62844d";
    private AuthenticationResult? authenticationResult;

    private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveAuthenticationProvider"/> class with the specified
    /// options for interactive authentication.
    /// </summary>
    /// <remarks>This constructor sets up the authentication provider using the provided options. If the
    /// client ID is not specified, a default client ID is used. The authentication flow can be customized with tenant
    /// ID and broker options.</remarks>
    /// <param name="options">The options used to configure the interactive authentication provider. Must include at least one scope and a
    /// valid client ID.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="options"/> does not specify any scopes.</exception>
    public InteractiveAuthenticationProvider(InteractiveAuthenticationProviderOptions options)
    {
        if (options.Scopes is null || options.Scopes.Length == 0)
        {
            throw new ArgumentException("Scopes are required", nameof(options.Scopes));
        }


        _options = options;
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            _options.ClientId = DefaultClientId;
        }

        var builder = PublicClientApplicationBuilder
            .Create(_options.ClientId)
            .WithDefaultRedirectUri();

        if (!string.IsNullOrWhiteSpace(_options.TenantId))
        {
            builder.WithTenantId(_options.TenantId);
        }
        else
        {
            builder.WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs);
        }

        if (_options.UseBroker)
        {
            builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows) { Title = "Better Graph" });
        }

        publicClientApplication = builder.Build();
    }

    private async Task LoadCache()
    {
        await semaphoreSlim.WaitAsync();
        if (CacheLoaded)
        {
            semaphoreSlim.Release();
            return;
        }
        var storageProperties = new StorageCreationPropertiesBuilder(".accounts", Path.Combine(Path.GetTempPath(), "better-graph"))
            .Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(publicClientApplication.UserTokenCache);
        CacheLoaded = true;
        semaphoreSlim.Release();
    }

    /// <summary>
    /// Gets an access token for the specified scopes, tenant ID, and user ID.
    /// </summary>
    /// <param name="scopes"></param>
    /// <param name="tenantId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AuthenticationResult> AccuireTokenAsync(IEnumerable<string> scopes, string? tenantId = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        if (!CacheLoaded)
            await LoadCache();

        if (authenticationResult is not null && authenticationResult.ExpiresOn > DateTimeOffset.UtcNow)
        {
            return authenticationResult;
        }

        var accounts = await publicClientApplication.GetAccountsAsync();
        bool tenantIsGuid = Guid.TryParse(tenantId, out _);
        var account = accounts.FirstOrDefault(a => (string.IsNullOrWhiteSpace(tenantId) || tenantIsGuid == false || a.HomeAccountId.TenantId == tenantId)
        && (string.IsNullOrEmpty(userId) || a.Username.Equals(userId, StringComparison.InvariantCultureIgnoreCase)));

        try
        {
            authenticationResult = account is null
                ? await publicClientApplication.AcquireTokenSilent(scopes, userId).ExecuteAsync(cancellationToken)
                : await publicClientApplication.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken);
            return authenticationResult;
        }
        catch (MsalUiRequiredException)
        {
            return await AcquireTokenInteractiveAsync(scopes, tenantId, account?.Username ?? userId, cancellationToken);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scopes"></param>
    /// <param name="tenantId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AuthenticationResult> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, string? tenantId = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        using var timeoutCancellation = new CancellationTokenSource(120000);

        // Create a "LinkedTokenSource" combining two CancellationTokens into one.
        using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

        if (!CacheLoaded)
            await LoadCache();
        //logger.LogInformation("Acquiring token interactively {@scopes} {tenantId} {userId}", scopes, tenantId, userId);
        var builder = publicClientApplication.AcquireTokenInteractive(scopes);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            builder = builder.WithTenantId(tenantId);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            builder = builder.WithLoginHint(userId);
        }

        if (_options.UseBroker)
        {
            builder = builder.WithParentActivityOrWindow(BrokerHandle.GetConsoleOrTerminalWindow());
        }

        return authenticationResult = await builder.ExecuteAsync(combinedCancellation.Token);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="additionalAuthenticationContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.URI.Host == "graph.microsoft.com")
        {
            var token = await AccuireTokenAsync(_options.Scopes!, _options.TenantId, _options.Username, cancellationToken);
            var headers = new RequestHeaders
            {
                { "Authorization", $"Bearer {token.AccessToken}" }
            };
            request.AddHeaders(headers);
        }
    }
}

/// <summary>
/// Represents the options for configuring an interactive authentication provider.
/// </summary>
/// <remarks>This class is used to specify the settings required for interactive authentication, including the
/// scopes for which access is requested, and optional parameters such as the client ID, tenant ID, and username. The
/// <see cref="UseBroker"/> property defaults to <see langword="true"/> on Windows platforms.</remarks>
public class InteractiveAuthenticationProviderOptions
{
    /// <summary>
    /// One or more scopes for which the access token is requested.
    /// </summary>
    public required string[] Scopes { get; set; }

    /// <summary>
    /// Should the authentication provider use a broker for authentication?
    /// </summary>
    public bool UseBroker { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Username to use for authentication, if applicable. Used for auto filling the login
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The tenant ID to use for authentication. If not specified, the default tenant is used.
    /// </summary>
    public string? TenantId { get; set; }
}

internal class BrokerHandle
{
    private enum GetAncestorFlags
    {
        GetParent = 1,
        GetRoot = 2,

        /// <summary>
        /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        /// </summary>
        GetRootOwner = 3
    }

    /// <summary>
    /// Retrieves the handle to the ancestor of the specified window.
    /// </summary>
    /// <param name="hwnd">A handle to the window whose ancestor will be retrieved.
    /// If this parameter is the desktop window, the function returns NULL. </param>
    /// <param name="flags">The ancestor to be retrieved.</param>
    /// <returns>The return value is the handle to the ancestor window.</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    // This is your window handle!
    public static IntPtr GetConsoleOrTerminalWindow()
    {
        IntPtr consoleHandle = GetConsoleWindow();
        IntPtr handle = GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

        return handle;
    }
}