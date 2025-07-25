using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Graph.Beta;

namespace Svrooij.BetterGraph.Plumbing;

public class GraphClientFactory
{

    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly HttpClient _httpClient;

    public GraphClientFactory(HttpClient httpClient, IAuthenticationProvider authenticationProvider)
    {
        _authenticationProvider = authenticationProvider ??
            throw new ArgumentNullException(nameof(authenticationProvider), "Authentication provider cannot be null.");
        _httpClient = httpClient;
    }

    public GraphServiceClient GetClient()
    {
        return new GraphServiceClient(new HttpClientRequestAdapter(_authenticationProvider, httpClient: _httpClient));
    }
}
