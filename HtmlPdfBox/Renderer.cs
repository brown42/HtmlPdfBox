using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace HtmlPdfBox;

public class Renderer
{
    private readonly ILogger<Renderer> _log;

    public Renderer(ILogger<Renderer> log, IOptions<AppSettings> options)
    {
        _log = log;
        var settings = options.Value;
        
        if (!Directory.Exists(settings.OutputPath))
        {
            Directory.CreateDirectory(settings.OutputPath);
        }
    }

    private string InjectBaseUrl(string html, string baseUrl)
    {
        // otherwise we need to inject a new one
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
            
        // remove any existing <base/> tag
        var bases = doc.DocumentNode.Descendants().Where(x => x.Name == "base").ToArray();
        foreach (var b in bases)
        {
            b.Remove();
        }
            
        // inject a new <base/> tag
        var head = doc.DocumentNode.SelectSingleNode("//head");
            
        // inject a <head/> tag
        if (head == null)
        {
            // find <body/> tag
            var body = doc.DocumentNode.SelectSingleNode("//body") ??
                       throw new InvalidOperationException("cannot inject a base element without at least a <body/> tag");
            
            // create <head/>
            head = doc.CreateElement("head");
            body.ParentNode.InsertBefore(head, body);
        }
        
        // create <base/>
        var baseTag = doc.CreateElement("base");
        baseTag.SetAttributeValue("href", baseUrl);
        head.AppendChild(baseTag);
        
        // serialize
        using var writer = new StringWriter();
        doc.Save(writer);
        return writer.ToString();
    }
    
    public async Task<byte[]> RenderAsync(RenderHtmlRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var browser = await GetBrowserAsync(cancellationToken);
        await using var page = await browser.NewPageAsync().WaitAsync(cancellationToken);
        var html = request.Html;
        if (request.BaseUrl != null)
        {
            html = InjectBaseUrl(html, request.BaseUrl);
        }

        await page.SetContentAsync(html).WaitAsync(cancellationToken);
        var pdf = await GetPageAsPdfAsync(page, cancellationToken);
        sw.Stop();
        _log.LogInformation("rendered html in {ms}ms", sw.ElapsedMilliseconds);
        return pdf;
    }

    public async Task<byte[]> RenderAsync(RenderUrlRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        await using var browser = await GetBrowserAsync(cancellationToken);
        await using var page = await browser.NewPageAsync().WaitAsync(cancellationToken);
        await page.GoToAsync(request.Url).WaitAsync(cancellationToken);
        var pdf = await GetPageAsPdfAsync(page, cancellationToken);
        sw.Stop();
        _log.LogInformation("rendered '{url}' in {ms}ms", request.Url, sw.ElapsedMilliseconds);
        return pdf;
    }

    private static async Task<byte[]> GetPageAsPdfAsync(IPage page, CancellationToken cancellationToken)
    {
        await page.EvaluateExpressionHandleAsync("document.fonts.ready").WaitAsync(cancellationToken);
        var pdf = await page.PdfDataAsync(new PdfOptions()
        {
            DisplayHeaderFooter = false
        }).WaitAsync(cancellationToken);
        return pdf;
    }

    private static async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        IBrowser? browser = null;
        
        try
        {
            browser = await Puppeteer.LaunchAsync(new LaunchOptions()
            {
                Args = ["--no-sandbox"],
                Headless = true,
            }).WaitAsync(cancellationToken);

            return browser;
        }
        catch
        {
            if (browser != null)
                await browser.DisposeAsync();
            
            throw;
        }
    }
}