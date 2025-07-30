using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Svrooij.PowerShell.DI;
using System.Management.Automation;
namespace Svrooij.BetterGraph.Commands;

/// <summary>
/// Get a Bearer token from the authentication provider.
/// </summary>
[Cmdlet(VerbsCommon.Get, "BgToken", DefaultParameterSetName = "Default")]
[OutputType(typeof(string))]
[GenerateBindings]
public partial class GetBgToken : DependencyCmdlet<GraphStartup>
{
    private const string AuthenticationScheme = "Bearer";

    /// <summary>
    /// Gets or sets the property to which the token is written, instead of writing to the output.
    /// </summary>
    [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "Write the token to this property, and do not write to output", ParameterSetName = "Default")]
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "Write the token as secure string to this property", ParameterSetName = nameof(AsSecureString))]
    public string? OutputProperty { get; set; }

    /// <summary>
    /// Should the token be returned as a secure string?
    /// </summary>
    /// <remarks>This makes the <see cref="OutputProperty"/> mandatory</remarks>
    [Parameter(Mandatory = false, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "Return the token as a secure string. This makes the OutputProperty mandatory.", ParameterSetName = nameof(AsSecureString))]
    public SwitchParameter AsSecureString { get; set; } = false;

    [ServiceDependency(Required = true)]
    private Microsoft.Kiota.Abstractions.Authentication.IAuthenticationProvider authProvider = default!;

    /// <inheritdoc/>
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(authProvider, cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            WriteError(new ErrorRecord(new InvalidOperationException("Failed to retrieve token. Did you call Connect-BgGraph prior to this?"), "TokenRetrievalFailed", ErrorCategory.InvalidOperation, null));
            return;
        }

        object outputValue = token;

        if (AsSecureString)
        {
            outputValue = ConvertToSecureString(token);
        }

        if (!string.IsNullOrEmpty(OutputProperty))
        {
            SessionState.PSVariable.Set(OutputProperty, outputValue);
        }
        else
        {
            WriteObject(token);
        }
    }

    private static System.Security.SecureString ConvertToSecureString(string plainText)
    {
        var secure = new System.Security.SecureString();
        foreach (char c in plainText)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }

    /// <summary>
    /// Asynchronously retrieves a token from the authentication provider.
    /// </summary>
    /// <param name="authenticationProvider"></param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the token string if successful; otherwise, null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the authentication provider is not set.</exception>
    internal static async ValueTask<string?> GetTokenAsync(IAuthenticationProvider authenticationProvider, CancellationToken cancellationToken = default)
    {
        if (authenticationProvider == null)
        {
            throw new InvalidOperationException("AuthenticationProvider is not set, please run Connect-BgGraph first.");
        }
        // This is a "hack" to get a token from the authentication provider.
        var ri = new RequestInformation(Method.GET, "https://graph.microsoft.com/test", new Dictionary<string, object>());
        await authenticationProvider.AuthenticateRequestAsync(ri, cancellationToken: cancellationToken);
        string? headerValue = ri.Headers.TryGetValue("Authorization", out var values) ? values.FirstOrDefault() : null;

        // Header should be in the format "Bearer <token>"
        // So we need to remove the "Bearer " part.
        int AuthenticationSchemeLength = AuthenticationScheme.Length + 1;
        return headerValue?.Length > AuthenticationSchemeLength && headerValue.StartsWith(AuthenticationScheme, StringComparison.InvariantCultureIgnoreCase) ? headerValue.Substring(AuthenticationSchemeLength) : null;
    }
}
