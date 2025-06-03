# EasyOpenTelemetry

A simplified library for adding OpenTelemetry observability to .NET applications with minimal configuration.

## Features

- 🚀 **Simple Setup**: Add comprehensive observability with just a few lines of code
- 📊 **Complete Observability**: Includes tracing, metrics, and logging
- ⚙️ **Flexible Configuration**: Environment-based or programmatic configuration
- 🔧 **Sensible Defaults**: Works out of the box with common scenarios
- 🎯 **Selective Instrumentation**: Enable/disable specific instrumentations as needed

## Quick Start

### Basic Usage

```csharp
// Program.cs
using OpenTelemetry.Simplified;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry with minimal configuration
builder.Services.AddSimplifiedOpenTelemetry(
    serviceName: "my-api",
    environment: "production",
    otlpEndpoint: "http://jaeger:4317"
);

// Add Serilog integration
builder.Host.UseSimplifiedSerilog(config => 
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
builder.Services.AddSimplifiedOpenTelemetry(otelConfig);
builder.Host.UseSimplifiedSerilog(otelConfig);
```

### Advanced Configuration

```csharp
builder.Services.AddSimplifiedOpenTelemetry(config =>
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

### Logging
- Serilog integration with OTLP sink. (This will only configure the OTEL sink for serilog. You can still setup serilog as you normally would, but this will ensure that the OTLP sink is configured correctly.)
- Automatic resource attribute mapping
- Configuration-based log levels

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
builder.Services.AddSimplifiedOpenTelemetry("my-service", "production", "http://jaeger:4317");
```