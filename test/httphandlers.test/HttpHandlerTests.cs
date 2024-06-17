using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Web;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers.Tests;

[Collection(nameof(SystemWebAdaptersHostedTests))]
public class HttpHandlerTests
{
    [Fact]
    public async void SyncHandlerSet()
    {
        // Arrange
        using var host = await CreateTestHost(middleware: app =>
        {
            app.Use((ctx, next) =>
            {
                ctx.AsSystemWeb().Handler = new Handler();
                return next(ctx);
            });
        });

        using var client = host.GetTestClient();

        // Act
        var result = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(DefaultResponse, result);
    }

    [Fact]
    public async void SyncHandlerRoute()
    {
        // Arrange
        using var host = await CreateTestHost(builder: services =>
        {
            services.AddHttpHandler<Handler>("/handler");
        });

        using var client = host.GetTestClient();

        // Act
        var result = await client.GetStringAsync("/handler");

        // Assert
        Assert.Equal(DefaultResponse, result);
    }

    [Fact]
    public async void AsyncHandlerSet()
    {
        // Arrange
        using var host = await CreateTestHost(middleware: app =>
        {
            app.Use((ctx, next) =>
            {
                ctx.AsSystemWeb().Handler = new AsyncHandler();
                return next(ctx);
            });
        });

        using var client = host.GetTestClient();

        // Act
        var result = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(DefaultResponse, result);
    }

    [Fact]
    public async void AsyncHandlerRoute()
    {
        // Arrange
        using var host = await CreateTestHost(builder: services =>
        {
            services.AddHttpHandler<AsyncHandler>("/handler");
        });

        using var client = host.GetTestClient();

        // Act
        var result = await client.GetStringAsync("/handler");

        // Assert
        Assert.Equal(DefaultResponse, result);
    }

    [Fact]
    public async void HandlerInModules()
    {
        // Arrange
        using var host = await CreateTestHost(builder: services =>
        {
            services.AddHttpApplication(options => options.RegisterModule<MyModule>());
        });

        using var client = host.GetTestClient();

        // Act
        var result = await client.GetStringAsync("/handler");

        // Assert
        Assert.Equal(DefaultResponse, result);
    }

    private Task<IHost> CreateTestHost(Action<IApplicationBuilder>? middleware = null, Action<ISystemWebAdapterBuilder>? builder = null)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHost(app =>
            {
                app.UseTestServer();
                app.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSession();
                    app.UseSystemWebAdapters();

                    middleware?.Invoke(app);

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHttpHandlers();
                    });
                });
            })
            .ConfigureServices(services =>
            {
                services.AddDistributedMemoryCache();
                services.AddRouting();

                var systemWebBuilder = services.AddSystemWebAdapters()
                    .AddWrappedAspNetCoreSession()
                    .AddHttpHandlers();

                builder?.Invoke(systemWebBuilder);
            })
            .StartAsync();
    }

    public const string DefaultResponse = "Hello world!";

    private sealed class Handler : IHttpHandler
    {
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write(DefaultResponse);
        }
    }

    private sealed class AsyncHandler : HttpTaskAsyncHandler
    {
        public override Task ProcessRequestAsync(HttpContext context)
        {
            return context.Response.Output.WriteAsync(DefaultResponse);
        }
    }

    private sealed class MyModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication application)
        {
            application.MapRequestHandler += (sender, o) =>
            {
                if (sender is HttpApplication { Context: { } context })
                {
                    if (context.Request.Path == "/handler")
                    {
                        context.Handler = new Handler();
                    }
                }
            };
        }
    }
}