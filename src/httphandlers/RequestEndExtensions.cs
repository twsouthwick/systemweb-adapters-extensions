// MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SystemWebAdapters.Features;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers;

internal static class RequestEndExtensions
{
    /// <summary>
    /// By design, the System.Web adapters try not to throw when <see cref="HttpResponse.End"/> but rather tracks it in other ways.
    /// However, WebForms was built with that expectation and some common scenarios (such as postback calls) break without this.
    /// </summary>
    public static IApplicationBuilder EnsureRequestEndThrows(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            await using var feature = new RequestEndThrowingFeature(ctx);

            ctx.Features.Set<IHttpResponseEndFeature>(feature);

            try
            {
                await next(ctx).ConfigureAwait(false);
            }
            catch (RequestEndException)
            {
            }
        });

    private sealed class RequestEndThrowingFeature : IHttpResponseEndFeature, IAsyncDisposable
    {
        private readonly HttpContextCore _context;
        private readonly IHttpResponseEndFeature _original;
        private bool _end;

        public RequestEndThrowingFeature(HttpContextCore context)
        {
            _context = context;
            _original = context.Features.GetRequiredFeature<IHttpResponseEndFeature>();
        }

        public bool IsEnded => _end || _original.IsEnded;

        public Task EndAsync()
        {
            _end = true;
            throw new RequestEndException();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            _context.Features.Set(_original);

            if (_end)
            {
                await _original.EndAsync().ConfigureAwait(false);
            }
        }
    }

    private sealed class RequestEndException : Exception
    {
    }
}
