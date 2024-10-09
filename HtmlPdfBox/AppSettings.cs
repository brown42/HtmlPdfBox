namespace HtmlPdfBox;

public class AppSettings
{
    public override string ToString()
    {
        return
            $"{nameof(ChromePath)}: {ChromePath}, " +
            $"{nameof(ChromeArgs)}: {string.Join(", ", ChromeArgs)}, " +
            $"{nameof(OutputPath)}: {OutputPath}, " +
            $"{nameof(MaxWorkers)}: {MaxWorkers}, " +
            $"{nameof(ApiKey)} Set: {ApiKey != null}, " +
            $"{nameof(InternalUrl)}: {InternalUrl}";
    }

    public string ChromePath { get; set; } = "";
    public string[] ChromeArgs { get; set; } = [];
    public string OutputPath { get; set; } = "";
    public int MaxWorkers { get; set; } = 1;
    public string? ApiKey { get; set; }
    public string InternalUrl { get; set; } = "";
}