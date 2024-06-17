// MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Swick.SystemWebAdapters.Extensions.HttpHandlers;

namespace Microsoft.AspNetCore.Builder;

public static class HttpHandlerBuilderExtensions
{
    public static IEndpointConventionBuilder MapHttpHandlers(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.DataSources.OfType<HttpHandlerEndpointConventionBuilder>().FirstOrDefault() is not { } existing)
        {
            existing = endpoints.ServiceProvider.GetRequiredService<HttpHandlerEndpointConventionBuilder>();
            endpoints.DataSources.Add(existing);
        }

        return existing;
    }
}
