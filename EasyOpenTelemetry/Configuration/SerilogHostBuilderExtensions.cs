using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using Serilog;
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
        
        return hostBuilder.UseSerilog((context, loggerConfiguration) =>
        {
            var protocol = configuration.Protocol == OtlpExportProtocol.Grpc 
                ? OtlpProtocol.Grpc 
                : OtlpProtocol.HttpProtobuf;

            loggerConfiguration
                .WriteTo.OpenTelemetry(
                    endpoint: configuration.OtlpEndpoint,
                    protocol: protocol,
                    resourceAttributes: new Dictionary<string, object>
                        {
                            ["deployment.environment"] = configuration.Environment,
                            ["service.name"] = configuration.ServiceName
                        }.Concat(configuration.AdditionalResourceAttributes)
                        .ToDictionary(x => x.Key, x => x.Value))
                .ReadFrom.Configuration(context.Configuration);
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
}