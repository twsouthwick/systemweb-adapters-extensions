# Extensions for Microsoft.AspNetCore.SystemWebAdapters

This project contains some helper extensions for migration ASP.NET Framework to ASP.NET Core application using the [System.Web Adapters](https://github.com/dotnet/systemweb-adapters).

## Implementation for `IHttpHandler`

v1.4.0 of the adapters brought support for the types used with `IHttpHandler` but did not provide an implementation to hook it up into ASP.NET Core. [This](./src/httphandlers) is an implementation that provides support for that:

### Set handlers manually

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSystemWebAdapters()
  .AddHttpHandlers();

var app = builder.Build();

app.UseWhen(ctx =>
    ctx.Request.Path == "/handler",
    app => app.Use((ctx, next) =>
    {
        ctx.AsSystemWeb().Handler = new Handler();
        return next(ctx);
    }));

app.UseSystemWebAdapters();

app.MapHttpHandlers();

app.Run();

sealed class Handler : IHttpHandler
{
    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        context.Response.Write("Hello world!");
    }
}
```

### Set handlers for a route

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSystemWebAdapters()
  .AddHttpHandler<Handler>("/handler");

var app = builder.Build();

app.UseSystemWebAdapters();

app.MapHttpHandlers();

app.Run();

sealed class Handler : IHttpHandler
{
    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        context.Response.Write("Hello world!");
    }
}
```

### Set handlers in a module

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSystemWebAdapters()
  .AddHttpApplication(options => options.RegisterModule<MyModule>());

var app = builder.Build();

app.UseSystemWebAdapters();

app.MapHttpHandlers();

app.Run();

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

sealed class Handler : IHttpHandler
{
    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        context.Response.Write("Hello world!");
    }
}
```
