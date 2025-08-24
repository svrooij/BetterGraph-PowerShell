using Microsoft.Extensions.Logging;
using Svrooij.BetterGraph.Plumbing;
using Svrooij.PowerShell.DI;
using System.Management.Automation;

namespace Svrooij.BetterGraph.Commands.Groups;
/// <summary>
/// <para type="synopsis">List groups or get single group</para>
/// <para type="description">\r\n\r\nThis is an [**authenticated command**](./authentication), so call [Connect-BgGraph](./Connect-BgGraph) before calling this command.</para>
/// </summary>
/// <psOrder>10</psOrder>
/// <example>
/// <para type="name">Get top 10 groups and select few parameters</para>
/// <para type="description">Get a list of first 10 groups in this tenant, and format the result as a table.</para>
/// <code>Get-BgGroup -Top 10 -Select Id, DisplayName, UserPrincipalName | Format-table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <example>
/// <para type="name">Get group by id</para>
/// <para type="description">Get a single group by id</para>
/// <code>Get-BgGroup -Id &quot;8195b446-e1dd-4064-a410-a1494d1ffe1b&quot; | Format-List</code>
/// </example>
/// <example>
/// <para type="name">Auto paging</para>
/// <para type="description">Let the module auto page over all results in pages of 10, you'll get the Groups async as long as you use them in a pipe</para>
/// <code>Get-BgGroup -Top 10 -All -Select Id, DisplayName, UserPrincipalName | Format-Table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <example>
/// <para type="name">Manual paging</para>
/// <para type="description">If you get groups and do not set the `-All` parameter, it will set the `$GetBgGroupNextLink` if there are more pages. Use this code to get the next page.</para>
/// <code>Get-BgGroup -NextLink $GetBgGroupNextLink | Format-Table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <parameterSet>
/// <para type="name">ById</para>
/// <para type="description">Get a single group by specifying the Id.</para>
/// </parameterSet>
///
/// <parameterSet>
/// <para type="name">Groups</para>
/// <para type="description">Get a list of groups, optionally filtered, selected, or paged.</para>
/// </parameterSet>
///
/// <parameterSet>
/// <para type="name">GroupsPaging</para>
/// <para type="description">Get the next page of groups using a NextLink from a previous response.</para>
/// </parameterSet>
[Cmdlet(VerbsCommon.Get, "BgGroup", DefaultParameterSetName = ParameterSetMultiple)]
[OutputType(typeof(Microsoft.Graph.Beta.Models.Group))]
[GenerateBindings]

public partial class GetBgGroup : DependencyCmdlet<GraphStartup>
{
    private const string ParameterSetSingle = "ById";
    private const string ParameterSetMultiple = "Groups";
    private const string ParameterSetMultiplePaging = "GroupsPaging";

    /// <summary>
    /// The unique identifier of the group to retrieve.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSetSingle, HelpMessage = "The unique identifier of the group to retrieve.")]
    public string? Id { get; set; }

    // --------------------- Multiple Users ---------------------
    /// <summary>
    /// Gets or sets the OData filter to apply when retrieving users.
    /// </summary>
    [Parameter(Mandatory = false, Position = 10, ParameterSetName = ParameterSetMultiple, HelpMessage = "OData filter to apply when retrieving groups.")]
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the properties to select for each user.
    /// </summary>
    [Parameter(Mandatory = false, Position = 11, ValueFromPipeline = false, ParameterSetName = ParameterSetMultiple, HelpMessage = "Properties to select for each group.")]
    [Parameter(Mandatory = false, Position = 11, ValueFromPipeline = false, ParameterSetName = ParameterSetSingle, HelpMessage = "Properties to select for the group.")]
    public string[]? Select { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of users to return.
    /// </summary>
    [Parameter(Mandatory = false, Position = 12, ParameterSetName = ParameterSetMultiple, HelpMessage = "Maximum number of groups to return.")]
    public int? Top { get; set; } = 25;

    /// <summary>
    /// Gets or sets a value indicating whether to retrieve all users using auto-paging.
    /// </summary>
    [Parameter(Mandatory = false, Position = 13, ParameterSetName = ParameterSetMultiple, HelpMessage = "Retrieve all groups using auto-paging.")]
    public SwitchParameter All { get; set; } = false;

    // ---------------------- Multiple Groups with Paging ---------------------

    /// <summary>
    /// Gets or sets the next link for paging through users.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSetMultiplePaging, HelpMessage = "The next link for paging through groups.")]
    public string? NextLink { get; set; }

    [ServiceDependency(Required = true)]
    private ILogger<GetBgGroup>? logger;

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient = default!;

    private SynchronizationContext? synchronizationContext;

    /// <inheritdoc />
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        synchronizationContext = SynchronizationContext.Current;
        logger?.LogDebug("Get-BgGroup parameter set: {ParameterSet}", ParameterSetName);
        // Make a switch on the ParameterSetName to determine which method to call
        switch (ParameterSetName)
        {
            case ParameterSetSingle:
                await GetSingleAsync(cancellationToken);
                break;
            case ParameterSetMultiple:
                await GetMultipleAsync(cancellationToken);
                break;
            case ParameterSetMultiplePaging:
                await GetMultiplePagingAsync(cancellationToken);
                break;
            default:
                logger?.LogError("Invalid parameter set: {ParameterSet}", ParameterSetName);
                throw new InvalidOperationException($"Invalid parameter set: {ParameterSetName}");
        }
    }

    private async Task GetSingleAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            logger?.LogError("Id parameter is required when using the ById parameter set.");
            throw new ArgumentException("Id cannot be null or empty.", nameof(Id));
        }
        logger?.LogDebug("Retrieving user with ID: {UserId}", Id);
        var group = await graphClient!.Groups[Id].GetAsync(req =>
        {
            req.QueryParameters.Select = Select;
        }, cancellationToken: cancellationToken);
        if (group != null)
        {
            logger?.LogDebug("Retrieved group: {DisplayName} ({Id})", group.DisplayName, group.Id);
            WriteObject(group);
        }
        else
        {
            logger?.LogWarning("No group found with ID: {Id}", Id);
        }
    }

    private async Task GetMultipleAsync(CancellationToken cancellationToken)
    {
        if (All.IsPresent)
        {
            logger?.LogInformation("Microsoft Graph API pager. Cancel with CTRL + C");
            var pageIterator = await graphClient!.CreatePageIteratorAsync<Microsoft.Graph.Beta.Models.Group, Microsoft.Graph.Beta.Models.GroupCollectionResponse>(
                graphClient!.Groups.ToGetRequestInformation(req =>
                {
                    req.QueryParameters.Select = Select;
                    req.QueryParameters.Top = Top;
                    req.QueryParameters.Filter = Filter;
                }),
                (group) =>
                {
                    if (synchronizationContext != null)
                    {
                        synchronizationContext.Post(_ => WriteObject(group), null);
                    }
                    else // This should not happen, but just in case
                    {
                        WriteObject(group);
                    }
                    //await Task.Delay(2000, CancellationToken.None);
                    return !cancellationToken.IsCancellationRequested;
                },
                cancellationToken: cancellationToken
            );

            await pageIterator.IterateAsync(cancellationToken);
            return;
        }

        var groups = await graphClient!.Groups.GetAsync(req =>
        {
            req.QueryParameters.Select = Select;
            req.QueryParameters.Top = Top;
            req.QueryParameters.Filter = Filter;
        }, cancellationToken: cancellationToken);
        if (groups?.OdataNextLink is not null)
        {
            SessionState.PSVariable.Set("GetBgGroupNextLink", groups.OdataNextLink);
        }
        if (groups?.Value != null)
        {
            logger?.LogDebug("Retrieved {GroupCount} users from Microsoft Graph API.", groups.Value.Count);
            WriteObject(groups.Value, true);
        }
        else
        {
            logger?.LogWarning("No groups found or an error occurred while retrieving users.");
        }
    }

    private async Task GetMultiplePagingAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NextLink))
        {
            logger?.LogError("NextLink parameter is required when using the GroupsPaging parameter set.");
            throw new ArgumentException("NextLink cannot be null or empty.", nameof(NextLink));
        }
        logger?.LogDebug("Retrieving groups with NextLink: {NextLink}", NextLink);
        var groups = await graphClient!.GetCollectionPageAsync<Microsoft.Graph.Beta.Models.GroupCollectionResponse>(
            NextLink,
            cancellationToken: cancellationToken
        );

        if (groups?.OdataNextLink is not null)
        {
            SessionState.PSVariable.Set("GetBgGroupNextLink", groups.OdataNextLink);
        }
        if (groups?.Value != null)
        {
            logger?.LogDebug("Retrieved {Count} groups from Microsoft Graph API with paging.", groups.Value.Count);
            WriteObject(groups.Value, true);
        }
        else
        {
            logger?.LogWarning("No groups found or an error occurred while retrieving groups with paging.");
        }
    }
}
