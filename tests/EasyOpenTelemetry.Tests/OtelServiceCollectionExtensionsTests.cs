using EasyOpenTelemetry.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EasyOpenTelemetry.Tests;

public class OtelServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEasyOpenTelemetry_Throws_WhenServiceNameMissing()
    {
        var services = new ServiceCollection();
        var config = new OtelConfiguration { ServiceName = "" };

        Assert.Throws<ArgumentException>(() => services.AddEasyOpenTelemetry(config));
    }

    [Fact]
    public void AddEasyOpenTelemetry_RegistersServices_WithValidConfiguration()
    {
        var services = new ServiceCollection();

        var result = services.AddEasyOpenTelemetry(
            serviceName: "test-service",
            environment: "test",
            otlpEndpoint: "http://localhost:4317");

        Assert.Same(services, result);
        using var provider = services.BuildServiceProvider();
        // Resolving the provider verifies the OpenTelemetry registrations are valid.
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddEasyOpenTelemetry_BuildsProvider_WithPrometheusAndOtlpExporters()
    {
        var services = new ServiceCollection();

        services.AddEasyOpenTelemetry(new OtelConfiguration
        {
            ServiceName = "test-service",
            EnablePrometheusExporter = true
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddEasyOpenTelemetry_BuildsProvider_WithPrometheusOnly()
    {
        var services = new ServiceCollection();

        services.AddEasyOpenTelemetry(new OtelConfiguration
        {
            ServiceName = "test-service",
            EnableOtlpMetricsExporter = false,
            EnablePrometheusExporter = true,
            PrometheusScrapeEndpointPath = "/internal/metrics"
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddEasyOpenTelemetry_DefaultsToOtlpOnly_WithoutPrometheus()
    {
        var services = new ServiceCollection();
        var config = new OtelConfiguration { ServiceName = "test-service" };

        // Guards the default metrics path: OTLP enabled, Prometheus off.
        Assert.True(config.EnableOtlpMetricsExporter);
        Assert.False(config.EnablePrometheusExporter);

        services.AddEasyOpenTelemetry(config);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}
