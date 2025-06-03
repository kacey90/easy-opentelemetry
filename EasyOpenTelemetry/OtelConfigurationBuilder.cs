using EasyOpenTelemetry.Configuration;

namespace EasyOpenTelemetry;

public static class OtelConfigurationBuilder
{
    public static OtelConfiguration Create(string serviceName, string environment, string otlpEndpoint)
    {
        return new OtelConfiguration
        {
            ServiceName = serviceName,
            Environment = environment,
            OtlpEndpoint = otlpEndpoint
        };
    }

    public static OtelConfiguration FromEnvironment(string? serviceName = null)
    {
        return new OtelConfiguration
        {
            ServiceName = serviceName ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "unknown-service",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development",
            OtlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317"
        };
    }
}