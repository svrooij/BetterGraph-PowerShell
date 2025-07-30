using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Svrooij.BetterGraph.Plumbing;

/// <summary>
/// Provides a static authentication mechanism using a pre-defined token.
/// </summary>
/// <remarks>This authentication provider adds a bearer token to the request headers for authorization. It is
/// suitable for scenarios where a fixed token is used for authentication.</remarks>
internal class StaticAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _token;

    public StaticAuthenticationProvider(string token)
    {
        _token = token;
    }

    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var headers = new RequestHeaders
        {
            { "Authorization", $"Bearer {_token}" }
        };
        request.AddHeaders(headers);
        return Task.CompletedTask;
    }
}
