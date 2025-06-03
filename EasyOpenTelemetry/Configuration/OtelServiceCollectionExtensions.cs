using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EasyOpenTelemetry.Configuration;

public static class OtelServiceCollectionExtensions
{
    public static IServiceCollection AddEasyOpenTelemetry(this IServiceCollection services, OtelConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ServiceName))
            throw new ArgumentException("ServiceName is required", nameof(configuration));
        
        var resourceAttributes = GetResourceAttributes(configuration);

        if (configuration.EnableTracing || configuration.EnableMetrics)
        {
            var otelBuilder = services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(configuration.ServiceName)
                    .AddAttributes(resourceAttributes));

            if (configuration.EnableTracing)
            {
                ConfigureTracing(otelBuilder, configuration, resourceAttributes);
            }

            if (configuration.EnableMetrics)
            {
                ConfigureMetrics(otelBuilder, configuration, resourceAttributes);
            }
        }

        return services;
    }
    
    public static IServiceCollection AddEasyOpenTelemetry(this IServiceCollection services, Action<OtelConfiguration> configure)
    {
        var configuration = new OtelConfiguration();
        configure(configuration);
        return services.AddEasyOpenTelemetry(configuration);
    }

    public static IServiceCollection AddEasyOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        string environment,
        string otlpEndpoint,
        OtlpExportProtocol protocol = OtlpExportProtocol.Grpc)
    {
        var configuration = new OtelConfiguration
        {
            ServiceName = serviceName,
            Environment = environment,
            OtlpEndpoint = otlpEndpoint,
            Protocol = protocol
        };
        return services.AddEasyOpenTelemetry(configuration);
    }

    private static void ConfigureMetrics(OpenTelemetryBuilder otelBuilder,
        OtelConfiguration configuration,
        Dictionary<string, object> resourceAttributes)
    {
        otelBuilder.WithMetrics(metrics =>
        {
            metrics.AddMeter(configuration.ServiceName);

            foreach (var meter in configuration.AdditionalMeterNames)
            {
                metrics.AddMeter(meter);
            }

            if (configuration.EnableAspNetCoreInstrumentation)
                metrics.AddAspNetCoreInstrumentation();

            if (configuration.EnableHttpClientInstrumentation)
                metrics.AddHttpClientInstrumentation();

            if (configuration.EnableRuntimeInstrumentation)
                metrics.AddRuntimeInstrumentation();

            if (configuration.EnableProcessInstrumentation)
                metrics.AddProcessInstrumentation();

            metrics.SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddService(configuration.ServiceName)
                    .AddAttributes(resourceAttributes))
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(configuration.OtlpEndpoint);
                    opts.Protocol = configuration.Protocol;
                });
        });
    }

    private static void ConfigureTracing(OpenTelemetryBuilder otelBuilder,
        OtelConfiguration configuration,
        Dictionary<string, object> resourceAttributes)
    {
        otelBuilder.WithTracing(trace =>
        {
            trace.AddSource(configuration.ServiceName);

            foreach (var source in configuration.AdditionalTracingSources)
            {
                trace.AddSource(source);
            }

            if (configuration.EnableAspNetCoreInstrumentation)
                trace.AddAspNetCoreInstrumentation();

            if (configuration.EnableHttpClientInstrumentation)
                trace.AddHttpClientInstrumentation();

            trace.SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddService(configuration.ServiceName)
                    .AddAttributes(resourceAttributes))
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(configuration.OtlpEndpoint);
                    opts.Protocol = configuration.Protocol;
                });
        });
    }

    private static Dictionary<string, object> GetResourceAttributes(OtelConfiguration configuration)
    {
        var attributes = new Dictionary<string, object>
        {
            ["deployment.environment"] = configuration.Environment,
            ["service.name"] = configuration.ServiceName
        };

        foreach (var attr in configuration.AdditionalResourceAttributes)
        {
            attributes[attr.Key] = attr.Value;
        }

        return attributes;
    }
}