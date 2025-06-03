using OpenTelemetry.Exporter;

namespace EasyOpenTelemetry.Configuration;

public class OtelConfiguration
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = "development";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;
    public Dictionary<string, object> AdditionalResourceAttributes { get; set; } = new();
    public bool EnableSerilogIntegration { get; set; } = true;
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;
    public bool EnableHttpClientInstrumentation { get; set; } = true;
    public bool EnableRuntimeInstrumentation { get; set; } = true;
    public bool EnableProcessInstrumentation { get; set; } = true;
    public List<string> AdditionalTracingSources { get; set; } = new();
    public List<string> AdditionalMeterNames { get; set; } = new();
}