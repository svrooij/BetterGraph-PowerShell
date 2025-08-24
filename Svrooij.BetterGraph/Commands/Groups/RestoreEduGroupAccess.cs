using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Beta.Models;
using Svrooij.PowerShell.DI;
using System.Management.Automation;

namespace Svrooij.BetterGraph.Commands.Groups;

/// <summary>
/// <para type="synopsis">Fix access for EduGroup</para>
/// <para type="description">In March 2025, Microsoft stopped unconnected access to edu groups, breaking edu functionallity like Assignments and Class notebook. This command fixes that.\r\n\r\nThis is an [**authenticated command**](./authentication), so call [Connect-BgGraph](./Connect-BgGraph) before calling this command.</para>
/// </summary>
/// <psOrder>100</psOrder>
/// <example>
/// <para type="name">Fix all groups</para>
/// <para type="description">Get all groups that match the filter, and process them.</para>
/// <code>Get-BgGroup -All -Top 50 -Select Id,DisplayName | Restore-EduGroupAccess </code>
/// </example>
/// <parameterSet>
/// <para type="name">Default</para>
/// <para type="description"></para>
/// </parameterSet>
[Cmdlet(VerbsData.Restore, "EduGroupAccess", DefaultParameterSetName = ParameterSetDefault)]
[OutputType(typeof(bool))]
[GenerateBindings]
public class RestoreEduGroupAccess : DependencyCmdlet<GraphStartup>
{
    private const string ParameterSetDefault = "Default";
    private readonly Dictionary<string, string> neededAccess = new Dictionary<string, string>
    {
        { "8f348934-64be-4bb2-bc16-c54c96789f43", "EDU Assignments" },
        { "22d27567-b3f0-4dc2-9ec2-46ed368ba538", "Reading Assignments" },
        { "2d4d3d8e-2be3-4bef-9f87-7875a61c29de","OneNote" },
        { "c9a559d2-7aab-4f13-a6ed-e7e9c52aec87","Microsoft Forms" },
        { "13291f5a-59ac-4c59-b0fa-d1632e8f3292","EDU OneNote" }
    };

    /// <summary>
    /// The unique identifier of the group to retrieve.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = ParameterSetDefault, HelpMessage = "The unique identifier of the group to fix.")]
    public string? Id { get; set; }

    [ServiceDependency(Required = true)]
    private ILogger<RestoreEduGroupAccess>? logger;

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient = default!;

    private SynchronizationContext? synchronizationContext;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        synchronizationContext = SynchronizationContext.Current;
        if (Id is null)
        {
            throw new ArgumentNullException(nameof(Id));
        }

        try
        {

            // Get site id
            var rootSite = await graphClient.Groups[Id].Sites["root"].GetAsync(cancellationToken: cancellationToken);
            if (rootSite is null || rootSite.Id is null)
            {
                logger?.LogWarning("Could not get root site for group {GroupId}", Id);
                return;
            }

            // Validate app permissions are not there
            var currentPermissions = await graphClient.Sites[rootSite.Id].Permissions.GetAsync(cancellationToken: cancellationToken);
            logger?.LogDebug("Current permissions: {Permissions}", currentPermissions?.Value?.Count ?? 0);

            var addPermissionsBatch = new BatchRequestContentCollection(graphClient);
            List<string> AppsToAdd = [];
            foreach (var access in neededAccess)
            {
                if (currentPermissions?.Value?.Any(p => p.GrantedToIdentities?.Any(i => i.Application?.Id == access.Key) == true) == true)
                {
                    logger?.LogDebug("Group {GroupId} already has access to {AppName}", Id, access.Value);
                    continue;
                }
                logger?.LogDebug("Adding access to {AppName} for group {GroupId}", access.Value, Id);
                AppsToAdd.Add(access.Value);
                await addPermissionsBatch.AddBatchRequestStepAsync(graphClient.Sites[rootSite.Id].Permissions.ToPostRequestInformation(new Microsoft.Graph.Beta.Models.Permission
                {
                    Roles = ["fullcontrol"],
                    GrantedToIdentities = [
                        new IdentitySet { Application = new Identity { Id = access.Key, DisplayName = access.Value } }
                        ]
                }));
            }

            if (addPermissionsBatch.BatchRequestSteps.Count > 0)
            {
                var response = await graphClient.Batch.PostAsync(addPermissionsBatch, cancellationToken: cancellationToken);
                logger?.LogInformation("Added {Apps} to group {GroupId}", string.Join(", ", AppsToAdd), Id);
            }
            else
            {
                logger?.LogDebug("No permissions to add for group {GroupId}", Id);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error processing group {GroupId}: {Message}", Id, ex.Message);
            return;

        }
    }
}
