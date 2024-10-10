namespace HtmlPdfBox;

public record RenderHtmlRequest(string Html, string? BaseUrl);

public record RenderUrlRequest(string Url);