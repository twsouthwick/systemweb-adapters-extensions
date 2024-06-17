// MIT License.

using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers;

internal static class HandlerExtensions
{
    internal static async ValueTask RunHandlerAsync(this IHttpHandler handler, HttpContextCore context)
    {
        // For handlers, we should ensure synchronous writing is allowed
        context.Features.GetRequiredFeature<IHttpBodyControlFeature>().AllowSynchronousIO = true;

        if (handler is HttpTaskAsyncHandler task)
        {
            await task.ProcessRequestAsync(context).ConfigureAwait(false);
        }
        else if (handler is IHttpAsyncHandler asyncHandler)
        {
            await Task.Factory.FromAsync((cb, state) => asyncHandler.BeginProcessRequest(context, cb, state), asyncHandler.EndProcessRequest, null).ConfigureAwait(false);
        }
        else
        {
            handler.ProcessRequest(context);
        }
    }

    public static RequestDelegate BuildDefaultHandlerDelegate(this IServiceProvider services)
    {
        var builder = new ApplicationBuilder(services);

        builder.EnsureRequestEndThrows();
        builder.Run(context =>
        {
            if (context.AsSystemWeb().CurrentHandler is { } handler)
            {
                return handler.RunHandlerAsync(context).AsTask();
            }

            context.Response.StatusCode = 500;
            return context.Response.WriteAsync("Invalid handler");
        });

        return builder.Build();
    }

    internal static Endpoint CreateEndpoint(this HttpContextCore core, IHttpHandler handler)
    {
        if (handler is Endpoint endpoint)
        {
            return endpoint;
        }

        var factory = core.RequestServices.GetRequiredService<IHttpHandlerEndpointFactory>();

        return factory.Create(handler);
    }

    internal static IHttpHandler CreateHandler(this HttpContextCore context, Endpoint endpoint)
    {
        if (endpoint is IHttpHandler handler)
        {
            return handler;
        }
        else if (endpoint.Metadata.GetMetadata<IHttpHandlerMetadata>() is { } metadata)
        {
            return metadata.Create(context);
        }
        else
        {
            return new EndpointHandler(endpoint);
        }
    }

    private sealed class EndpointHandler : HttpTaskAsyncHandler
    {
        public EndpointHandler(Endpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public Endpoint Endpoint { get; }

        public override Task ProcessRequestAsync(System.Web.HttpContext context)
        {
            if (Endpoint.RequestDelegate is { } request)
            {
                return request(context);
            }

            return Task.CompletedTask;
        }
    }
}

