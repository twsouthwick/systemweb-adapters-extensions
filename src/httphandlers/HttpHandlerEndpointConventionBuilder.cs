// MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using System.Web;
using System.Web.SessionState;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers;

internal sealed class HttpHandlerEndpointConventionBuilder : EndpointDataSource, IEndpointConventionBuilder
{
    private readonly IHttpHandlerCollection[] _managers;
    private readonly RequestDelegate _defaultHandler;

    private List<Action<EndpointBuilder>> _conventions = [];

    public HttpHandlerEndpointConventionBuilder(
        IEnumerable<IHttpHandlerCollection> managers,
        IServiceProvider services)
    {
        _managers = managers.ToArray();
        _defaultHandler = services.BuildDefaultHandlerDelegate();
    }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            var endpoints = new List<Endpoint>();

            foreach (var (route, metadataCollection) in CollectMetadata())
            {
                var pattern = RoutePatternFactory.Parse(route);
                var builder = new RouteEndpointBuilder(_defaultHandler, pattern, 0);

                builder.AddHandler(metadataCollection);

                foreach (var convention in _conventions)
                {
                    convention(builder);
                }

#if NET7_0_OR_GREATER
                if (builder.FilterFactories.Count > 0)
                {
                    throw new NotSupportedException("Filter factories are not supported for handlers");
                }
#endif

                endpoints.Add(builder.Build());
            }

            return endpoints;
        }
    }

    private Dictionary<string, List<object>> CollectMetadata()
    {
        var metadataCollection = new Dictionary<string, List<object>>();
        var mappedRoutes = new List<NamedHttpHandlerRoute>();

        foreach (var manager in _managers)
        {
            mappedRoutes.AddRange(manager.NamedRoutes);

            foreach (var metadata in manager.GetHandlerMetadata())
            {
                metadataCollection.Add(metadata.Route, [metadata]);
            }
        }

        foreach (var mappedRoute in mappedRoutes)
        {
            if (metadataCollection.TryGetValue(mappedRoute.Path, out var fromCollection) && fromCollection is [IHttpHandlerMetadata handler, ..])
            {
                metadataCollection.Add(mappedRoute.Route, [.. fromCollection, new MappedHandlerMetadata(mappedRoute.Route, handler)]);
            }
        }

        return metadataCollection;
    }

    public void Add(Action<EndpointBuilder> convention)
        => (_conventions ??= []).Add(convention);

    public override IChangeToken GetChangeToken() => new CompositeChangeToken(_managers.Select(m => m.GetChangeToken()).ToArray());

    private sealed class MappedHandlerMetadata(string route, IHttpHandlerMetadata metadata) : IHttpHandlerMetadata
    {
        public SessionStateBehavior Behavior => metadata.Behavior;

        public string Route => route;

        public IHttpHandler Create(HttpContextCore context) => metadata.Create(context);
    }
}
