using System.Management.Automation;
using Microsoft.Graph.Beta;
using Svrooij.PowerShell.DI;
namespace Svrooij.BetterGraph.Commands.Users;

/// <summary>
/// <para type="synopsis">Remove a user from Microsoft Graph.</para>
/// <para type="description">
/// This is an <b>authenticated command</b>, so call <c>Connect-BgGraph</c> before using this command.
/// </para>
/// </summary>
/// <psOrder>30</psOrder>
/// <example>
/// <para type="name">Remove a user by ID</para>
/// <para type="description">Delete a user by specifying their unique ID.</para>
/// <code>Remove-BgUser -UserId "8195b446-e1dd-4064-a410-a1494d1ffe1b"</code>
/// </example>
/// <parameterSet>
/// <para type="name">Default</para>
/// <para type="description">Remove a user by specifying the <c>UserId</c>.</para>
/// </parameterSet>
[Cmdlet(VerbsCommon.Remove, "BgUser")]
[OutputType(typeof(bool))]
[GenerateBindings]
public partial class RemoveBgUser : DependencyCmdlet<GraphStartup>
{
    [Parameter(Mandatory = true, Position = 0)]
    public string? UserId { get; set; }

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient;
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        await graphClient!.Users[UserId!].DeleteAsync(cancellationToken: cancellationToken);
        WriteObject(true);
    }
}