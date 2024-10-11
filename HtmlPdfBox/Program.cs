using HtmlPdfBox;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// configure services
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton<Renderer>();

// configure app pipeline
var app = builder.Build();
app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();

// log settings
var options = app.Services.GetRequiredService<IOptions<AppSettings>>();
app.Logger.LogInformation("running app settings: {settings}", options.Value);

// get services
var renderer = app.Services.GetRequiredService<Renderer>();
var workerCount = Math.Max(1, options.Value.MaxWorkers);
using var semaphore = new SemaphoreSlim(workerCount, workerCount);
using var httpClient = new HttpClient();

// configure auth middleware
if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
{
    app.Logger.LogWarning("YOU HAVE NOT CONFIGURED AN API KEY, ANYBODY CAN ACCESS THIS");
}
else
{
    app.Logger.LogInformation("Configuring API key middleware");
    app.Use(async (context, next) =>
    {
        var authenticated = false;
        
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var key))
        {
            authenticated = string.Equals(options.Value.ApiKey,
                key.FirstOrDefault(),
                StringComparison.InvariantCulture);
        }

        if (authenticated)
        {
            await next(context);
        }
        else
        {
            app.Logger.LogError("Invalid X-Api-Key header, denying request to '{url}'", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    });
}

// routes
app.MapPost("/render/html", async ([FromBody] RenderHtmlRequest request, CancellationToken cancellationToken) =>
{
    await AcquireWorkerAsync(cancellationToken);

    try
    {
        var data = await renderer.RenderAsync(request, cancellationToken);
        return Results.File(data, contentType: "application/pdf", "rendered.pdf");
    }
    finally
    {
        ReleaseWorker();
    }
});

app.MapPost("/render/url", async ([FromBody] RenderUrlRequest request, CancellationToken cancellationToken) =>
{
    await AcquireWorkerAsync(cancellationToken);

    try
    {
        var data = await renderer.RenderAsync(request, cancellationToken);
        return Results.File(data, contentType: "application/pdf", "rendered.pdf");
    }
    finally
    {
        ReleaseWorker();
    }
});

app.Run();
return;

// local functions
async Task AcquireWorkerAsync(CancellationToken cancellationToken)
{
    app.Logger.LogInformation("Waiting for worker ({count} free)", semaphore.CurrentCount);
    await semaphore.WaitAsync(cancellationToken);
    app.Logger.LogInformation("Acquired worker ({count} free)", semaphore.CurrentCount);
}

void ReleaseWorker()
{
    semaphore.Release();
    app.Logger.LogInformation("Released worker ({count} free)", semaphore.CurrentCount);
}