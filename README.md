# EasyOpenTelemetry

A simplified library for adding OpenTelemetry observability to .NET applications with minimal configuration.

## Features

- 🚀 **Simple Setup**: Add comprehensive observability with just a few lines of code
- 📊 **Complete Observability**: Includes tracing, metrics, and logging
- ⚙️ **Flexible Configuration**: Environment-based or programmatic configuration
- 🔧 **Sensible Defaults**: Works out of the box with common scenarios
- 🎯 **Selective Instrumentation**: Enable/disable specific instrumentations as needed

## Installation
Install the NuGet package:

```bash
dotnet add package EasyOpenTelemetry
```

## Quick Start

### Basic Usage

```csharp
// Program.cs
using EasyOpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry with minimal configuration
builder.Services.AddEasyOpenTelemetry(
    serviceName: "my-api",
    environment: "production",
    otlpEndpoint: "http://jaeger:4317"
);

// Add Serilog integration
builder.Host.UseEasyOpenTelemetryWithSerilog(config => 
{
    config.ServiceName = "my-api";
    config.Environment = "production";
    config.OtlpEndpoint = "http://jaeger:4317";
});

var app = builder.Build();
app.Run();
```

### Environment-Based Configuration

```csharp
// Uses environment variables: OTEL_SERVICE_NAME, ASPNETCORE_ENVIRONMENT, OTEL_EXPORTER_OTLP_ENDPOINT
var otelConfig = OtelConfigurationBuilder.FromEnvironment("my-service");
builder.Services.AddEasyOpenTelemetry(otelConfig);
builder.Host.UseEasyOpenTelemetryWithSerilog(otelConfig);
```

### Advanced Configuration

```csharp
builder.Services.AddEasyOpenTelemetry(config =>
{
    config.ServiceName = "my-advanced-api";
    config.Environment = "staging";
    config.OtlpEndpoint = "https://otlp.example.com:4317";
    config.Protocol = OtlpExportProtocol.HttpProtobuf;
    config.AdditionalResourceAttributes["service.version"] = "1.2.3";
    config.AdditionalTracingSources.Add("MyCustomLibrary");
    config.EnableProcessInstrumentation = false;
});
```

## Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `ServiceName` | *required* | Name of your service |
| `Environment` | `"development"` | Deployment environment |
| `OtlpEndpoint` | `"http://localhost:4317"` | OTLP collector endpoint |
| `Protocol` | `Grpc` | Export protocol (Grpc or HttpProtobuf) |
| `EnableTracing` | `true` | Enable distributed tracing |
| `EnableMetrics` | `true` | Enable metrics collection |
| `EnableOtlpMetricsExporter` | `true` | Push metrics to the OTLP endpoint |
| `EnablePrometheusExporter` | `false` | Expose a Prometheus scrape endpoint (pull). See [Prometheus Metrics](#prometheus-metrics-beta) |
| `PrometheusScrapeEndpointPath` | `"/metrics"` | Path served by the Prometheus scrape endpoint |
| `EnableSerilogIntegration` | `true` | Enable Serilog OTLP sink |
| `EnableAspNetCoreInstrumentation` | `true` | Instrument ASP.NET Core |
| `EnableHttpClientInstrumentation` | `true` | Instrument HTTP client calls |
| `EnableRuntimeInstrumentation` | `true` | Collect .NET runtime metrics |
| `EnableProcessInstrumentation` | `true` | Collect process metrics |

## Environment Variables

The library automatically reads these environment variables when using `FromEnvironment()`:

- `OTEL_SERVICE_NAME`: Service name
- `ASPNETCORE_ENVIRONMENT`: Environment name
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP endpoint URL

## Included Instrumentations

### Tracing
- ASP.NET Core (HTTP requests)
- HttpClient (outbound HTTP calls)
- Custom sources via `AdditionalTracingSources`

### Metrics
- ASP.NET Core (request metrics)
- HttpClient (HTTP client metrics)
- .NET Runtime (GC, thread pool, etc.)
- Process metrics (CPU, memory)
- Custom meters via `AdditionalMeterNames`
- Export via OTLP (push) and/or an opt-in Prometheus scrape endpoint (pull) — see [Prometheus Metrics](#prometheus-metrics-beta)

### Logging
- Serilog integration with OTLP sink. (This will only configure the OTEL sink for serilog. You can still setup serilog as you normally would, but this will ensure that the OTLP sink is configured correctly.)
- Automatic resource attribute mapping
- Configuration-based log levels

## Prometheus Metrics (beta)

In addition to (or instead of) pushing metrics over OTLP, you can expose a Prometheus
**scrape endpoint** for Prometheus to pull from. This is opt-in — flip one flag.

> ⚠️ **Beta dependency.** This feature is powered by `OpenTelemetry.Exporter.Prometheus.AspNetCore`,
> which is published only as a **prerelease** (`1.15.3-beta.1`) by the OpenTelemetry project. As a
> result, EasyOpenTelemetry now carries this prerelease dependency. The package also targets the
> ASP.NET Core shared framework (it already did, transitively, via the ASP.NET Core instrumentation).

### Usage

```csharp
using EasyOpenTelemetry;
using EasyOpenTelemetry.Configuration;

var builder = WebApplication.CreateBuilder(args);

var otelConfig = OtelConfigurationBuilder.Create("my-api", "production", "http://collector:4317");
otelConfig.EnablePrometheusExporter = true;          // expose /metrics for Prometheus to scrape
// otelConfig.EnableOtlpMetricsExporter = false;     // optional: pull-only, skip OTLP push
// otelConfig.PrometheusScrapeEndpointPath = "/internal/metrics"; // optional: custom path

builder.Services.AddEasyOpenTelemetry(otelConfig);

var app = builder.Build();

// REQUIRED: map the scrape endpoint. Without this call no endpoint is served
// (it's a silent no-op when EnablePrometheusExporter is false, so the same code is
// safe across environments).
app.UseEasyOpenTelemetryPrometheus(otelConfig);

app.Run();
```

Prometheus is a **pull** exporter, so `OtlpEndpoint`/`Protocol` do not apply to it. OTLP push and
Prometheus pull can run side by side (the default), or you can run Prometheus-only by setting
`EnableOtlpMetricsExporter = false`.

> **Note:** Prometheus requires `EnableMetrics = true` (the default). If metrics are disabled,
> the exporter is never registered.

## Migration from Manual Setup

Replace this:
```csharp
// Old manual setup (20+ lines)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(applicationName))
    .WithTracing(trace => trace
        .AddSource(applicationName)
        .AddAspNetCoreInstrumentation()
        // ... more configuration
    );
```

With this:
```csharp
// New simplified setup (1 line)
builder.Services.AddEasyOpenTelemetry("my-service", "production", "http://jaeger:4317");
```