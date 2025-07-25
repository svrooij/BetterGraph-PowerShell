using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace Svrooij.BetterGraph.Plumbing;

internal static class GraphServiceClientExtensions
{
    internal static async Task<PageIterator<TEnity, TCollectionPage>> CreatePageIteratorAsync<TEnity, TCollectionPage>(
        this GraphServiceClient client,
        RequestInformation requestInfo,
        Func<TEnity, bool> callback,
        CancellationToken cancellationToken)
        where TEnity : IParsable
        where TCollectionPage : IParsable, IAdditionalDataHolder, new()
    {
        var page0 = await client.GetCollectionPageAsync<TCollectionPage>(
            requestInfo,
            errorMapping: null,
            cancellationToken: cancellationToken
        );

        return PageIterator<TEnity, TCollectionPage>.CreatePageIterator(
            client,
            page0!,
            callback
        );
    }

    internal static async Task<PageIterator<TEnity, TCollectionPage>> CreatePageIteratorAsync<TEnity, TCollectionPage>(
    this GraphServiceClient client,
    RequestInformation requestInfo,
    Func<TEnity, Task<bool>> asyncCallback,
    CancellationToken cancellationToken)
    where TEnity : IParsable
    where TCollectionPage : IParsable, IAdditionalDataHolder, new()
    {
        var page0 = await client.GetCollectionPageAsync<TCollectionPage>(
            requestInfo,
            errorMapping: null,
            cancellationToken: cancellationToken
        );

        return PageIterator<TEnity, TCollectionPage>.CreatePageIterator(
            client,
            page0!,
            asyncCallback
        );
    }

    //private static Dictionary<string, ParsableFactory<IParsable>> GetDefaultErrorMapping()
    //{
    //    return new Dictionary<string, ParsableFactory<IParsable>>()
    //    {
    //        {"XXX", (parsable) => new ServiceException(ErrorConstants.Messages.PageIteratorRequestError,new Exception(parsable.GetErrorMessage())) },
    //    };
    //}

    internal static async Task<TCollectionPage?> GetCollectionPageAsync<TCollectionPage>(
        this GraphServiceClient client,
        RequestInformation requestInfo,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default)
        where TCollectionPage : IParsable, IAdditionalDataHolder, new()
    {
        return await client.RequestAdapter.SendAsync<TCollectionPage>(requestInfo, (parseNode) => new TCollectionPage(), errorMapping, cancellationToken);
    }

    internal static async Task<TCollectionPage?> GetCollectionPageAsync<TCollectionPage>(
        this GraphServiceClient client,
        string nextLink,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default)
        where TCollectionPage : IParsable, IAdditionalDataHolder, new()
    {
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = nextLink,
            PathParameters = new Dictionary<string, object>()
        };

        return await client.GetCollectionPageAsync<TCollectionPage>(requestInfo, errorMapping, cancellationToken);
    }
}

