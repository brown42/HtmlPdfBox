using HtmlPdfBox;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton<Renderer>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();

var options = app.Services.GetRequiredService<IOptions<AppSettings>>();
var renderer = app.Services.GetRequiredService<Renderer>();
using var httpClient = new HttpClient();

app.Logger.LogInformation("running app settings: {settings}", options.Value);

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
        else
        {
            authenticated = context.Request.Path.StartsWithSegments("/html");
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

app.MapGet("/html/{id}", (Guid id) =>
{
    var html = renderer.GetHtml(id);
    return Results.Content(html, "text/html");
});

app.MapPost("/render/html", async ([FromBody] RenderHtmlRequest request, CancellationToken cancellationToken) =>
{
    var data = await renderer.RenderAsync(request, cancellationToken);
    return Results.File(data, contentType: "application/pdf", "rendered.pdf");
});

app.MapPost("/render/url", async ([FromBody] RenderUrlRequest request, CancellationToken cancellationToken) =>
{
    var data = await renderer.RenderAsync(request, cancellationToken);
    return Results.File(data, contentType: "application/pdf", "rendered.pdf");
});

app.Run();