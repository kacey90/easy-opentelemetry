using OpenTelemetry.Exporter;
using Serilog.Core;
using Serilog.Events;

namespace EasyOpenTelemetry.Configuration;

public class OtlpLogExporterOptions
{
    /// <summary>Label for diagnostics only.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Full logs URL, e.g. https://host/v1/logs for HTTP.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.HttpProtobuf;

    /// <summary>Sent with every export, e.g. Authorization: Bearer ….</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Only records carrying ALL of these properties are exported. Binds from config.</summary>
    public List<string> RequiredProperties { get; set; } = new();

    /// <summary>Optional code-only filter, applied after RequiredProperties.</summary>
    public Func<LogEvent, bool>? Filter { get; set; }

    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Verbose;

    /// <summary>Properties stripped before export (enrichment noise, PII).</summary>
    public List<string> ExcludeProperties { get; set; } = new();

    /// <summary>Properties added to every exported record if absent.</summary>
    public Dictionary<string, object> AddProperties { get; set; } = new();

    /// <summary>Send the message template as an attribute. Off is better for analytics backends.</summary>
    public bool IncludeMessageTemplate { get; set; } = true;

    public bool Enabled { get; set; } = true;
}

internal sealed class RemovePropertiesEnricher(params IEnumerable<string> names) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var name in names) 
            logEvent.RemovePropertyIfPresent(name);
    }
}