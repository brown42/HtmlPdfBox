using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace HtmlPdfBox;

public class Renderer
{
    private readonly ILogger<Renderer> _log;
    private readonly AppSettings _settings;
    private readonly ConcurrentDictionary<Guid, string> _htmlCache = new();

    public Renderer(ILogger<Renderer> log, IOptions<AppSettings> options)
    {
        _log = log;
        _settings = options.Value;
        
        if (!Directory.Exists(_settings.OutputPath))
        {
            Directory.CreateDirectory(_settings.OutputPath);
        }
    }

    public string GetHtml(Guid id)
    {
        if (!_htmlCache.TryGetValue(id, out var html)) throw new InvalidOperationException($"invalid html id {id}");
        return html;
    }
    
    public async Task<byte[]> RenderAsync(RenderHtmlRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var id = Guid.NewGuid();
        _log.LogInformation("rendering html request {id}", id);
        
        try
        {
            CacheHtml(id, request.Html);
            var filename = GetFileName(id);
            var url = $"{_settings.InternalUrl}/html/{id}";
            var data = await RenderUrlAsync(url, filename, cancellationToken);
            File.Delete(filename);
            return data;
        }
        finally
        {
            ClearCache(id);
            sw.Stop();
            _log.LogInformation("rendering html completed in {ms}ms", sw.ElapsedMilliseconds);
        }
    }

    public async Task<byte[]> RenderAsync(RenderUrlRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var id = Guid.NewGuid();
        _log.LogInformation("rendering url request {id} for url '{url}'", id, request.Url);
        
        try
        {
            var filename = GetFileName(id);
            var data = await RenderUrlAsync(request.Url, filename, cancellationToken);
            File.Delete(filename);
            return data;
        }
        finally
        {
            sw.Stop();
            _log.LogInformation("rendering url request {id} completed in {ms}ms", id, sw.ElapsedMilliseconds);
        }
    }

    private async Task<byte[]> RenderUrlAsync(string url, string filename, CancellationToken cancellationToken)
    {
        var args = new List<string>(_settings.ChromeArgs)
        {
            $"--print-to-pdf={filename}",
            url
        };
        var process = Process.Start(new ProcessStartInfo(_settings.ChromePath, args)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = false,
        });

        if (process == null)
        {
            throw new InvalidOperationException("unable to create chrome process");
        }
        
        await process.WaitForExitAsync(cancellationToken);
        var data = await File.ReadAllBytesAsync(filename, cancellationToken);

        return data;
    }

    private string GetFileName(Guid id)
    {
        return Path.Combine(_settings.OutputPath, $"{id:N}.pdf");
    }

    private void CacheHtml(Guid id, string html)
    {
        _htmlCache[id] = html;
    }

    private void ClearCache(Guid id)
    {
        _htmlCache.Remove(id, out _);
    }
}