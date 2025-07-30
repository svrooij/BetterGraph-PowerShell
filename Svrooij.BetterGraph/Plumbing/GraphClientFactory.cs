using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Graph.Beta;

namespace Svrooij.BetterGraph.Plumbing;

/// <summary>
/// Helper class to create authenticated instances of GraphServiceClient.
/// </summary>
public class GraphClientFactory
{

    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="authenticationProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public GraphClientFactory(HttpClient httpClient, IAuthenticationProvider? authenticationProvider)
    {
        _authenticationProvider = authenticationProvider ??
            throw new ArgumentNullException(nameof(authenticationProvider), "Authentication provider cannot be null.");
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="GraphServiceClient"/> configured with the specified
    /// authentication provider and HTTP client.
    /// </summary>
    /// <remarks>The returned <see cref="GraphServiceClient"/> is initialized with an <see
    /// cref="HttpClientRequestAdapter"/> that uses the provided authentication provider and HTTP client. Ensure that
    /// the authentication provider is properly configured to handle authentication for requests made by the
    /// client.</remarks>
    /// <returns>A <see cref="GraphServiceClient"/> instance configured for making requests to the Microsoft Graph API.</returns>
    public GraphServiceClient GetClient()
    {
        return new GraphServiceClient(new HttpClientRequestAdapter(_authenticationProvider, httpClient: _httpClient));
    }
}
