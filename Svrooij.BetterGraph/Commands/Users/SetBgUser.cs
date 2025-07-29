using System.Management.Automation;
using Microsoft.Graph.Beta;
using Svrooij.PowerShell.DI;
namespace Svrooij.BetterGraph.Commands.Users;

/// <summary>
/// <para type="synopsis">Update an existing user in Microsoft Graph.</para>
/// <para type="description">
/// This is an <b>authenticated command</b>, so call <c>Connect-BgGraph</c> before using this command.
/// Use this command to modify properties of an existing user by specifying their <c>UserId</c> and a <c>User</c> object with updated values.
/// </para>
/// </summary>
/// <psOrder>40</psOrder>
/// <example>
/// <para type="name">Update a user's display name</para>
/// <para type="description">Change the display name of a user by specifying their ID and a user object with the new display name.</para>
/// <code>
/// $user = [Microsoft.Graph.Beta.Models.User]::new()
/// # Or get the user from Get-BgUser and modify it
/// $user.DisplayName = "New Display Name"
/// Set-BgUser -UserId "8195b446-e1dd-4064-a410-a1494d1ffe1b" -User $user</code>
/// </example>
/// <parameterSet>
/// <para type="name">Default</para>
/// <para type="description">Update a user by specifying the <c>UserId</c> and a <c>User</c> object containing the properties to update.</para>
/// </parameterSet>
[Cmdlet(VerbsCommon.Set, "BgUser")]
[OutputType(typeof(Microsoft.Graph.Beta.Models.User))]
[GenerateBindings]
public partial class SetBgUser : DependencyCmdlet<GraphStartup>
{
    /// <summary>
    /// Gets or sets the unique identifier of the user to update.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, HelpMessage = "The unique identifier of the user to update.")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user object containing updated properties.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "The user object containing updated properties.")]
    public Microsoft.Graph.Beta.Models.User? User { get; set; }

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient;
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        var updatedUser = await graphClient!.Users[UserId!].PatchAsync(User!, cancellationToken: cancellationToken);
        WriteObject(updatedUser);
    }
}
