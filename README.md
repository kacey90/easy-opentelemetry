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
| `AdditionalResourceAttributes` | empty | Extra resource attributes on traces, metrics and every log sink (override `service.name` / `deployment.environment` on collision) |
| `AdditionalTracingSources` | empty | Extra `ActivitySource` names to trace |
| `AdditionalMeterNames` | empty | Extra `Meter` names to collect |
| `AdditionalLogExporters` | empty | Extra, filtered OTLP log destinations. See [Additional Log Exporters](#additional-log-exporters) |

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
- Optional extra, filtered OTLP log destinations. See [Additional Log Exporters](#additional-log-exporters)

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

## Additional Log Exporters

Besides the primary OTLP sink (which receives every log), you can send a **filtered, reshaped
copy** of your logs to more OTLP endpoints, such as a product-analytics or vendor backend that
only wants business events. Each exporter is its own Serilog sub-logger, so whatever it filters,
adds or strips **never affects the primary sink**. With no exporters configured (the default),
behaviour is unchanged.

```csharp
using EasyOpenTelemetry.Configuration;
using EasyOpenTelemetry.Logging;
using OpenTelemetry.Exporter;

builder.Host.UseEasyOpenTelemetryWithSerilog(config =>
{
    config.ServiceName = "my-api";
    config.OtlpEndpoint = "http://collector:4317";        // primary sink: everything

    config.AddLogExporter("analytics", exporter =>
    {
        exporter.Endpoint = "https://otlp.vendor.example/v1/logs"; // full logs URL
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;       // the default
        exporter.Headers["Authorization"] = $"Bearer {builder.Configuration["Analytics:ApiKey"]}";

        // Only export records that carry ALL of these properties.
        exporter.RequiredProperties.Add(EventLoggerExtensions.EventNameAttribute); // "event.name"

        exporter.ExcludeProperties.Add("RequestPath");        // strip noise / PII
        exporter.AddProperties["source"] = "backend";         // added only if absent
        exporter.IncludeMessageTemplate = false;              // see note below
    });
});

// Anywhere you have an ILogger: emits a log record with an "event.name" attribute.
logger.LogEvent("checkout.completed", [new("cart.total", 42.5)]);
```

Analytics backends (Produlytics-style) usually want `IncludeMessageTemplate = false`: they treat
each record as an event and its attributes as event properties, so the message-template attribute
is just noise.

| `OtlpLogExporterOptions` property | Default | Description |
|----------|---------|-------------|
| `Name` | (set by `AddLogExporter`) | Label used in error messages |
| `Endpoint` | empty | Full `http(s)` logs URL. **Required while `Enabled`**: startup throws `ArgumentException` if it is missing or not absolute |
| `Protocol` | `HttpProtobuf` | `HttpProtobuf` or `Grpc` |
| `Headers` | empty | Sent with every export, e.g. `Authorization` |
| `RequiredProperties` | empty | Only records carrying **all** of these properties are exported |
| `Filter` | `null` | Optional `Func<LogEvent, bool>`, applied on top of `RequiredProperties` |
| `MinimumLevel` | `Verbose` | Minimum level for this exporter |
| `ExcludeProperties` | empty | Properties removed before export |
| `AddProperties` | empty | Properties added to every exported record if absent |
| `IncludeMessageTemplate` | `true` | Send the message template as an attribute |
| `Enabled` | `true` | Turn the exporter off without removing it (e.g. in environments with no endpoint) |

Things to know:

- **Only `UseEasyOpenTelemetryWithSerilog` uses exporters.** `AddEasyOpenTelemetry` ignores
  `AdditionalLogExporters`, and so does `EnableSerilogIntegration = false`.
- **Options are read once, at startup.** Changing an exporter's options after the host is built
  has no effect.
- **Filters see the original record.** `RequiredProperties`, `Filter` and `MinimumLevel` are
  checked before `AddProperties` / `ExcludeProperties` run. A property you add can't satisfy
  `RequiredProperties`, and a property you strip is still visible to `Filter`.
- **`MinimumLevel` can only narrow.** An exporter never sees records below the logger's own
  minimum level (e.g. from `Serilog:MinimumLevel` in configuration).
- **Resource attributes are shared.** Every exporter gets the same `service.name`,
  `deployment.environment` and `AdditionalResourceAttributes` as the primary sink.
- **`OTEL_EXPORTER_OTLP_*` environment variables don't apply.** They describe the primary
  collector, so additional exporters ignore them and only use their own `Endpoint`/`Headers`.

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