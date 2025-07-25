using Microsoft.Graph.Beta;
using System.Management.Automation;
using Svrooij.PowerShell.DI;

namespace Svrooij.BetterGraph.Commands.Users;

/// <summary>
/// <para type="synopsis">Create a new user in Microsoft Graph.</para>
/// <para type="description">
/// This is an <b>authenticated command</b>, so call <c>Connect-BgGraph</c> before using this command.
/// </para>
/// </summary>
/// <psOrder>20</psOrder>
/// <example>
/// <para type="name">Create a simple user</para>
/// <para type="description">Create a user with a display name, user principal name, and password.</para>
/// <code>New-BgUser -UserPrincipalName "john.doe@contoso.com" -DisplayName "John Doe" -Password "P@ssw0rd!"</code>
/// </example>
/// <example>
/// <para type="name">Create a user from a user object</para>
/// <para type="description">Create a user by passing a pre-configured <c>User</c> object.</para>
/// <code>$user = [Microsoft.Graph.Beta.Models.User]::new()
/// $user.UserPrincipalName = "jane.doe@contoso.com"
/// $user.DisplayName = "Jane Doe"
/// New-BgUser -User $user -Password "AnotherP@ssw0rd!"</code>
/// </example>
///
/// <parameterSet>
/// <para type="name">Simple</para>
/// <para type="description">Create a user by specifying <c>UserPrincipalName</c>, <c>DisplayName</c>, and <c>Password</c> directly.</para>
/// </parameterSet>
///
/// <parameterSet>
/// <para type="name">Object</para>
/// <para type="description">Create a user by passing a <c>User</c> object, optionally with a <c>Password</c>.</para>
/// </parameterSet>
[Cmdlet(VerbsCommon.New, "BgUser")]
[OutputType(typeof(Microsoft.Graph.Beta.Models.User))]
[GenerateBindings]
public partial class NewBgUser : DependencyCmdlet<GraphStartup>
{
    private const string ParameterSetSimple = "Simple";
    private const string ParameterSetObject = "Object";

    [Parameter(Mandatory = true, Position = 1, ParameterSetName = ParameterSetSimple)]
    public string UserPrincipalName { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2, ParameterSetName = ParameterSetSimple)]
    public string DisplayName { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 3, ParameterSetName = ParameterSetSimple)]
    [Parameter(Mandatory = false, Position = 2, ParameterSetName = ParameterSetObject)]
    public string? Password { get; set; }

    [Parameter(Mandatory = true, Position = 1, ParameterSetName = ParameterSetObject)]
    public Microsoft.Graph.Beta.Models.User? User { get; set; }

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient;
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        var user = ParameterSetName == ParameterSetSimple ? new Microsoft.Graph.Beta.Models.User
        {
            UserPrincipalName = UserPrincipalName,
            DisplayName = DisplayName,
            
        }
        : User ?? throw new ArgumentNullException(nameof(User), "User cannot be null when using the Object parameter set.");
        // Ensure the PasswordProfile is set, if not provided, generate a random password
        user.PasswordProfile ??= new Microsoft.Graph.Beta.Models.PasswordProfile
        {
            Password = Password ?? Guid.NewGuid().ToString() + "$@!",
            ForceChangePasswordNextSignIn = true
        };
        var createdUser = await graphClient!.Users.PostAsync(user, cancellationToken: cancellationToken);
        WriteObject(createdUser);
    }
}
