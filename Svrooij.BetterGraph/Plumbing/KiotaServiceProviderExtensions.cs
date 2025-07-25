using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Svrooij.BetterGraph.Plumbing;

internal static class KiotaServiceProviderExtensions
{
    internal static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
    {
        // Add Kiota handlers here, e.g.:
        // services.AddTransient<IRequestHandler, MyRequestHandler>();
        var handlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
        foreach (var handler in handlers)
        {
            services.AddTransient(handler);
        }
        return services;
    }

    public static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
    {
        // Dynamically load the Kiota handlers from the Client Factory
        var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
        // And attach them to the http client builder
        foreach (var handler in kiotaHandlers)
        {
            builder.AddHttpMessageHandler((sp) => (DelegatingHandler)sp.GetRequiredService(handler));
        }

        return builder;
    }
}
