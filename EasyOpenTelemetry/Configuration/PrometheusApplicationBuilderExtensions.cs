using Microsoft.AspNetCore.Builder;

namespace EasyOpenTelemetry.Configuration;

public static class PrometheusApplicationBuilderExtensions
{
    /// <summary>
    /// Maps the OpenTelemetry Prometheus scraping endpoint when
    /// <see cref="OtelConfiguration.EnablePrometheusExporter"/> is enabled. The endpoint path is
    /// taken from <see cref="OtelConfiguration.PrometheusScrapeEndpointPath"/> (configured on the
    /// exporter). When the exporter is disabled this is a no-op so the same startup code works in
    /// every environment.
    /// </summary>
    public static IApplicationBuilder UseEasyOpenTelemetryPrometheus(
        this IApplicationBuilder app,
        OtelConfiguration configuration)
    {
        if (configuration.EnablePrometheusExporter)
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }
}
