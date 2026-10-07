using EasyOpenTelemetry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyOpenTelemetry.Tests;

public class SerilogHostBuilderExtensionsTests
{
    private static void BuildLogger(OtelConfiguration config)
    {
        using var host = new HostBuilder()
            .UseEasyOpenTelemetryWithSerilog(config)
            .Build();

        // The Serilog pipeline is built lazily; resolving the logger runs the configuration callback.
        Assert.NotNull(host.Services.GetRequiredService<Serilog.ILogger>());
    }

    [Fact]
    public void BuildsLogger_WhenAdditionalResourceAttributesOverrideDefaults()
    {
        var config = new OtelConfiguration { ServiceName = "test-service" };
        config.AdditionalResourceAttributes["service.name"] = "override";
        config.AdditionalResourceAttributes["deployment.environment"] = "override";

        BuildLogger(config);
    }

    [Fact]
    public void BuildsLogger_WithAdditionalLogExporters()
    {
        var config = new OtelConfiguration { ServiceName = "test-service" }
            .AddLogExporter("vendor", e =>
            {
                e.Endpoint = "http://localhost:4318/v1/logs";
                e.Headers["Authorization"] = "Bearer test";
                e.RequiredProperties.Add("event.name");
                e.IncludeMessageTemplate = false;
            })
            .AddLogExporter("disabled-without-endpoint", e => e.Enabled = false);

        BuildLogger(config);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/v1/logs")]
    [InlineData("otlp.vendor.example/v1/logs")]
    [InlineData("ftp://otlp.vendor.example/v1/logs")]
    public void Throws_WhenEnabledExporterHasNoAbsoluteHttpEndpoint(string endpoint)
    {
        var config = new OtelConfiguration { ServiceName = "test-service" }
            .AddLogExporter("vendor", e => e.Endpoint = endpoint);

        // Fails at registration, before the host is built.
        var ex = Assert.Throws<ArgumentException>(() => new HostBuilder().UseEasyOpenTelemetryWithSerilog(config));
        Assert.Contains("'vendor'", ex.Message);
    }

    [Fact]
    public void DoesNotValidateExporters_WhenSerilogIntegrationIsDisabled()
    {
        var config = new OtelConfiguration { ServiceName = "test-service", EnableSerilogIntegration = false }
            .AddLogExporter("vendor", _ => { });

        new HostBuilder().UseEasyOpenTelemetryWithSerilog(config);
    }
}
