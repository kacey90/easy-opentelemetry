using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Configuration;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

namespace EasyOpenTelemetry.Configuration;

public static class SerilogHostBuilderExtensions
{
    public static IHostBuilder UseEasyOpenTelemetryWithSerilog(
        this IHostBuilder hostBuilder,
        OtelConfiguration configuration)
    {
        if (!configuration.EnableSerilogIntegration)
            return hostBuilder;

        foreach (var exporter in configuration.AdditionalLogExporters)
            ValidateLogExporter(exporter);

        return hostBuilder.UseSerilog((context, loggerConfiguration) =>
        {
            var resourceAttributes = OtelResourceAttributes.From(configuration);

            loggerConfiguration
                .WriteTo.OpenTelemetry(
                    endpoint: configuration.OtlpEndpoint,
                    protocol: ToSerilogProtocol(configuration.Protocol),
                    resourceAttributes: resourceAttributes)
                .ReadFrom.Configuration(context.Configuration);

            foreach (var exporter in configuration.AdditionalLogExporters)
            {
                if (!exporter.Enabled)
                    continue;

                var headers = new Dictionary<string, string>(exporter.Headers);
                loggerConfiguration.WriteTo.AdditionalLogExporter(exporter, sink => sink.OpenTelemetry(o =>
                {
                    o.LogsEndpoint = exporter.Endpoint;
                    o.Protocol = ToSerilogProtocol(exporter.Protocol);
                    o.Headers = headers;
                    o.ResourceAttributes = resourceAttributes; // same as the primary sink
                    if (!exporter.IncludeMessageTemplate)
                        o.IncludedData &= ~IncludedData.MessageTemplateTextAttribute;
                },
                // OTEL_EXPORTER_OTLP_* describe the primary collector; never let them redirect a vendor sink.
                ignoreEnvironment: true));
            }
        });
    }

    public static IHostBuilder UseEasyOpenTelemetryWithSerilog(
        this IHostBuilder hostBuilder,
        Action<OtelConfiguration> configureOptions)
    {
        var configuration = new OtelConfiguration();
        configureOptions(configuration);
        return hostBuilder.UseEasyOpenTelemetryWithSerilog(configuration);
    }

    /// <summary>
    /// Adds a sub-logger that filters and reshapes events for one additional exporter, then hands them to
    /// <paramref name="configureSink"/>. Sub-loggers receive a copy of each event, so nothing done here is
    /// visible to the primary sink.
    /// </summary>
    internal static LoggerConfiguration AdditionalLogExporter(
        this LoggerSinkConfiguration writeTo,
        OtlpLogExporterOptions exporter,
        Action<LoggerSinkConfiguration> configureSink)
    {
        // Serilog runs enrichers before filters within a logger, so filtering and reshaping live in nested
        // sub-loggers: filters must see the original event, not one already touched by Add/ExcludeProperties.
        return writeTo.Logger(filtered =>
        {
            filtered.MinimumLevel.Is(exporter.MinimumLevel);

            foreach (var requiredProperty in exporter.RequiredProperties)
                filtered.Filter.ByIncludingOnly(Matching.WithProperty(requiredProperty));

            if (exporter.Filter is not null)
                filtered.Filter.ByIncludingOnly(exporter.Filter);

            filtered.WriteTo.Logger(reshaped =>
            {
                reshaped.MinimumLevel.Verbose();

                foreach (var (key, value) in exporter.AddProperties)
                    reshaped.Enrich.WithProperty(key, value);

                if (exporter.ExcludeProperties.Count > 0)
                    // Snapshot: like the filters above, later changes to the options must not affect a built logger.
                    reshaped.Enrich.With(new RemovePropertiesEnricher([.. exporter.ExcludeProperties]));

                configureSink(reshaped.WriteTo);
            });
        });
    }

    private static void ValidateLogExporter(OtlpLogExporterOptions exporter)
    {
        if (!exporter.Enabled)
            return;

        // Scheme check matters: on Unix a bare path like "/v1/logs" parses as an absolute file:// URI.
        if (!Uri.TryCreate(exporter.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException(
                $"Log exporter '{exporter.Name}' is enabled but its Endpoint '{exporter.Endpoint}' is not an absolute http(s) URL. " +
                "Set Endpoint, or set Enabled = false to turn the exporter off.",
                nameof(OtelConfiguration.AdditionalLogExporters));
    }

    private static OtlpProtocol ToSerilogProtocol(OtlpExportProtocol protocol) =>
        protocol == OtlpExportProtocol.Grpc ? OtlpProtocol.Grpc : OtlpProtocol.HttpProtobuf;
}
