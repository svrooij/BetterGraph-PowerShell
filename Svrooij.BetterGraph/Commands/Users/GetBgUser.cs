using Microsoft.Extensions.Logging;
using Microsoft.Graph.Beta;
using Microsoft.Kiota.Abstractions;
using Svrooij.BetterGraph.Plumbing;
using Svrooij.PowerShell.DI;
using System.Management.Automation;

namespace Svrooij.BetterGraph.Commands.Users;
/// <summary>
/// <para type="synopsis">List users or get single user</para>
/// <para type="description">\r\n\r\nThis is an [**authenticated command**](./authentication), so call [Connect-BgGraph](./Connect-BgGraph) before calling this command.</para>
/// </summary>
/// <psOrder>10</psOrder>
/// <example>
/// <para type="name">Get top 10 users and select few parameters</para>
/// <para type="description">Get a list of first 10 users in this tenant, and format the result as a table.</para>
/// <code>Get-BgUser -Top 10 -Select Id, DisplayName, UserPrincipalName | Format-table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <example>
/// <para type="name">Get user by id</para>
/// <para type="description">Get a single user by id (or user principal name)</para>
/// <code>Get-BgUser -UserId &quot;8195b446-e1dd-4064-a410-a1494d1ffe1b&quot; | Format-List</code>
/// </example>
/// <example>
/// <para type="name">Auto paging</para>
/// <para type="description">Let the module auto page over all results in pages of 10, you'll get the Users async as long as you use them in a pipe</para>
/// <code>Get-BgUser -Top 10 -All -Select Id, DisplayName, UserPrincipalName | Format-Table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <example>
/// <para type="name">Manual paging</para>
/// <para type="description">If you get users and do not set the `-All` parameter, it will set the `$GetBgUserNextLink` if there are more pages. Use this code to get the next page.</para>
/// <code>Get-BgUser -NextLink $GetBgUserNextLink | Format-Table -Property Id, DisplayName, UserPrincipalName</code>
/// </example>
/// <parameterSet>
/// <para type="name">ById</para>
/// <para type="description">Get a single user by specifying the UserId.</para>
/// </parameterSet>
///
/// <parameterSet>
/// <para type="name">Users</para>
/// <para type="description">Get a list of users, optionally filtered, selected, or paged.</para>
/// </parameterSet>
///
/// <parameterSet>
/// <para type="name">UsersPaging</para>
/// <para type="description">Get the next page of users using a NextLink from a previous response.</para>
/// </parameterSet>
// Get-BgUser supports Paging, but graph does NOT support paging by using a Skip parameter in the request, in fact it does paging by creating a NextLink in the response.
[Cmdlet(VerbsCommon.Get, "BgUser", DefaultParameterSetName = ParameterSetMultiple)]
[OutputType(typeof(Microsoft.Graph.Beta.Models.User))]
[GenerateBindings]

public partial class GetBgUser : DependencyCmdlet<GraphStartup>
{
    private const string ParameterSetSingle = "ById";
    private const string ParameterSetMultiple = "Users";
    private const string ParameterSetMultiplePaging = "UsersPaging";

    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSetSingle)]
    public string? UserId { get; set; }

    // --------------------- Multiple Users ---------------------
    [Parameter(Mandatory = false, Position = 10, ParameterSetName = ParameterSetMultiple)]
    public string? Filter { get; set; }
    [Parameter(Mandatory = false, Position = 11, ValueFromPipeline = false, ParameterSetName = ParameterSetMultiple)]
    [Parameter(Mandatory = false, Position = 11, ValueFromPipeline = false, ParameterSetName = ParameterSetSingle)]
    public string[]? Select { get; set; }

    [Parameter(Mandatory = false, Position = 12, ParameterSetName = ParameterSetMultiple)]
    public int? Top { get; set; } = 25;

    [Parameter(Mandatory = false, Position = 13, ParameterSetName = ParameterSetMultiple)]
    public SwitchParameter All { get; set; } = false;


    // ---------------------- Multiple Users with Paging ---------------------

    [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSetMultiplePaging)]
    public string? NextLink { get; set; }


    [ServiceDependency(Required = true)]
    private ILogger<GetBgUser>? logger;

    [ServiceDependency(Required = true)]
    private Microsoft.Graph.Beta.GraphServiceClient graphClient;

    private SynchronizationContext? synchronizationContext;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        synchronizationContext = SynchronizationContext.Current;
        logger?.LogDebug("Get-BgUser parameter set: {ParameterSet}", ParameterSetName);
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
        if (string.IsNullOrWhiteSpace(UserId))
        {
            logger?.LogError("UserId parameter is required when using the ById parameter set.");
            throw new ArgumentException("UserId cannot be null or empty.", nameof(UserId));
        }
        logger?.LogDebug("Retrieving user with ID: {UserId}", UserId);
        var user = await graphClient!.Users[UserId].GetAsync(req =>
        {
            req.QueryParameters.Select = Select;
        }, cancellationToken: cancellationToken);
        if (user != null)
        {
            logger?.LogDebug("Retrieved user: {DisplayName} ({Id})", user.DisplayName, user.Id);
            WriteObject(user);
        }
        else
        {
            logger?.LogWarning("No user found with ID: {UserId}", UserId);
        }
    }

    private async Task GetMultipleAsync(CancellationToken cancellationToken)
    {
        if (All.IsPresent)
        {
            logger?.LogInformation("Microsoft Graph API pager. Cancel with CTRL + C");
            var pageIterator = await graphClient!.CreatePageIteratorAsync<Microsoft.Graph.Beta.Models.User, Microsoft.Graph.Beta.Models.UserCollectionResponse>(
                graphClient!.Users.ToGetRequestInformation(req =>
                {
                    req.QueryParameters.Select = Select;
                    req.QueryParameters.Top = Top;
                    req.QueryParameters.Filter = Filter;
                }),
                async (user) =>
                {
                    if (synchronizationContext != null)
                    {
                        synchronizationContext.Post(_ => WriteObject(user), null);
                    }
                    else // This should not happen, but just in case
                    {
                        WriteObject(user);
                    }
                    //await Task.Delay(2000, CancellationToken.None);
                    return !cancellationToken.IsCancellationRequested;
                },
                cancellationToken: cancellationToken
            );

            await pageIterator.IterateAsync(cancellationToken);
            return;
        }

        var users = await graphClient!.Users.GetAsync(req =>
        {
            req.QueryParameters.Select = Select;
            req.QueryParameters.Top = Top;
            req.QueryParameters.Filter = Filter;
        }, cancellationToken: cancellationToken);
        if (users?.OdataNextLink is not null)
        {
            SessionState.PSVariable.Set("GetBgUserNextLink", users.OdataNextLink);
        }
        if (users?.Value != null)
        {
            logger?.LogDebug("Retrieved {UserCount} users from Microsoft Graph API.", users.Value.Count);
            WriteObject(users.Value, true);
        }
        else
        {
            logger?.LogWarning("No users found or an error occurred while retrieving users.");
        }
    }

    private async Task GetMultiplePagingAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NextLink))
        {
            logger?.LogError("NextLink parameter is required when using the UsersPaging parameter set.");
            throw new ArgumentException("NextLink cannot be null or empty.", nameof(NextLink));
        }
        logger?.LogDebug("Retrieving users with NextLink: {NextLink}", NextLink);
        var users = await graphClient!.GetCollectionPageAsync<Microsoft.Graph.Beta.Models.UserCollectionResponse>(
            NextLink,
            cancellationToken: cancellationToken
        );

        if (users?.OdataNextLink is not null)
        {
            SessionState.PSVariable.Set("GetBgUserNextLink", users.OdataNextLink);
        }
        if (users?.Value != null)
        {
            logger?.LogDebug("Retrieved {UserCount} users from Microsoft Graph API with paging.", users.Value.Count);
            WriteObject(users.Value, true);
        }
        else
        {
            logger?.LogWarning("No users found or an error occurred while retrieving users with paging.");
        }
    }
}
