using EasyOpenTelemetry.Configuration;
using EasyOpenTelemetry.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace EasyOpenTelemetry.Tests;

public class AdditionalLogExporterTests
{
    private static (Logger Logger, CollectingSink Primary, CollectingSink Exported) CreatePipeline(
        OtlpLogExporterOptions exporter)
    {
        var primary = new CollectingSink();
        var exported = new CollectingSink();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(primary)
            .WriteTo.AdditionalLogExporter(exporter, sink => sink.Sink(exported))
            .CreateLogger();

        return (logger, primary, exported);
    }

    [Fact]
    public void RequiredProperties_ExportsOnlyEventsCarryingAllOfThem()
    {
        var exporter = new OtlpLogExporterOptions { RequiredProperties = ["UserId", "Plan"] };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger.Information("No properties");
            logger.Information("Only {UserId}", 1);
            logger.Information("Both {UserId} {Plan}", 1, "pro");
        }

        var single = Assert.Single(exported.Events);
        Assert.Equal("Both {UserId} {Plan}", single.MessageTemplate.Text);
        Assert.Equal(3, primary.Events.Count);
    }

    [Fact]
    public void Filter_IsAppliedOnTopOfRequiredProperties()
    {
        var exporter = new OtlpLogExporterOptions
        {
            RequiredProperties = ["UserId"],
            Filter = e => e.Level >= LogEventLevel.Warning
        };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger.Information("Info {UserId}", 1);
            logger.Warning("Warning without user");
            logger.Warning("Warning {UserId}", 1);
        }

        var single = Assert.Single(exported.Events);
        Assert.Equal("Warning {UserId}", single.MessageTemplate.Text);
        Assert.Equal(3, primary.Events.Count);
    }

    [Fact]
    public void MinimumLevel_OnlyNarrowsTheExporter()
    {
        var exporter = new OtlpLogExporterOptions { MinimumLevel = LogEventLevel.Error };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger.Debug("debug");
            logger.Information("info");
            logger.Error("error");
        }

        Assert.Equal(LogEventLevel.Error, Assert.Single(exported.Events).Level);
        Assert.Equal(3, primary.Events.Count);
    }

    [Fact]
    public void ExcludeProperties_AreRemovedFromExportedEvents_ButNotFromThePrimarySink()
    {
        var exporter = new OtlpLogExporterOptions { ExcludeProperties = ["Email", "RequestPath"] };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger
                .ForContext("RequestPath", "/signup")
                .Information("Signed up {UserId} {Email}", 42, "someone@example.com");
        }

        var exportedEvent = Assert.Single(exported.Events);
        Assert.False(exportedEvent.Properties.ContainsKey("Email"));
        Assert.False(exportedEvent.Properties.ContainsKey("RequestPath"));
        Assert.True(exportedEvent.Properties.ContainsKey("UserId"));

        var primaryEvent = Assert.Single(primary.Events);
        Assert.True(primaryEvent.Properties.ContainsKey("Email"));
        Assert.True(primaryEvent.Properties.ContainsKey("RequestPath"));
        Assert.True(primaryEvent.Properties.ContainsKey("UserId"));
    }

    [Fact]
    public void AddProperties_AreAddedWhenAbsent_WithoutOverwritingOrLeakingToThePrimarySink()
    {
        var exporter = new OtlpLogExporterOptions
        {
            AddProperties = new() { ["Source"] = "backend", ["Plan"] = "free" }
        };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger.Information("Upgraded to {Plan}", "pro");
        }

        var exportedEvent = Assert.Single(exported.Events);
        Assert.Equal("\"backend\"", exportedEvent.Properties["Source"].ToString());
        Assert.Equal("\"pro\"", exportedEvent.Properties["Plan"].ToString());

        var primaryEvent = Assert.Single(primary.Events);
        Assert.False(primaryEvent.Properties.ContainsKey("Source"));
    }

    [Fact]
    public void Filters_SeeTheOriginalEvent_NotTheReshapedOne()
    {
        var exporter = new OtlpLogExporterOptions
        {
            // An added property must not satisfy RequiredProperties...
            RequiredProperties = ["Tenant"],
            AddProperties = new() { ["Tenant"] = "default" },
            // ...and an excluded property must still be visible to Filter.
            ExcludeProperties = ["Internal"],
            Filter = e => !e.Properties.ContainsKey("Internal")
        };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        {
            logger.Information("No tenant");
            logger.Information("Internal {Tenant} {Internal}", "acme", true);
            logger.Information("Exported {Tenant}", "acme");
        }

        var single = Assert.Single(exported.Events);
        Assert.Equal("Exported {Tenant}", single.MessageTemplate.Text);
        Assert.Equal(3, primary.Events.Count);
    }

    [Fact]
    public void LogEvent_IsSelectedByRequiringTheEventNameAttribute()
    {
        var exporter = new OtlpLogExporterOptions { RequiredProperties = [EventLoggerExtensions.EventNameAttribute] };
        var (logger, primary, exported) = CreatePipeline(exporter);

        using (logger)
        using (var factory = new SerilogLoggerFactory(logger))
        {
            var msLogger = factory.CreateLogger("test");
            msLogger.LogEvent("checkout.completed", [new("cart.total", 42.5)]);
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(msLogger, "Ordinary log line");
        }

        var exportedEvent = Assert.Single(exported.Events);
        Assert.Equal("\"checkout.completed\"",
            exportedEvent.Properties[EventLoggerExtensions.EventNameAttribute].ToString());
        Assert.Equal("42.5", exportedEvent.Properties["cart.total"].ToString());
        Assert.Equal(2, primary.Events.Count);
    }

    [Theory]
    [InlineData("checkout.completed")]
    [InlineData("odd.{name}")]
    public void LogEvent_UsesEventNameAsTemplate_WithoutSyntheticStateProperty(string eventName)
    {
        var sink = new CollectingSink();

        using (var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger())
        using (var factory = new SerilogLoggerFactory(logger))
        {
            factory.CreateLogger("test").LogEvent(eventName, [new("cart.total", 42.5)]);
        }

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal($"Event {eventName}", logEvent.RenderMessage());
        // Braces in the event name are escaped, not parsed as template holes.
        Assert.Empty(logEvent.MessageTemplate.Tokens.OfType<Serilog.Parsing.PropertyToken>());
        Assert.DoesNotContain("State", logEvent.Properties.Keys);
        Assert.DoesNotContain("{OriginalFormat}", logEvent.Properties.Keys);
        Assert.Equal(
            new HashSet<string> { EventLoggerExtensions.EventNameAttribute, "cart.total", "SourceContext" },
            logEvent.Properties.Keys.ToHashSet());
    }

    [Fact]
    public void ExcludeProperties_AreSnapshottedWhenTheLoggerIsBuilt()
    {
        var exporter = new OtlpLogExporterOptions { ExcludeProperties = ["Email"] };
        var (logger, _, exported) = CreatePipeline(exporter);

        exporter.ExcludeProperties.Clear();
        using (logger)
        {
            logger.Information("Signed up {Email}", "someone@example.com");
        }

        Assert.DoesNotContain("Email", Assert.Single(exported.Events).Properties.Keys);
    }

    [Fact]
    public void AddLogExporter_AppendsNamedExporter_AndReturnsSameConfiguration()
    {
        var config = new OtelConfiguration();

        var result = config.AddLogExporter("vendor", e => e.Endpoint = "https://vendor.example/v1/logs");

        Assert.Same(config, result);
        var exporter = Assert.Single(config.AdditionalLogExporters);
        Assert.Equal("vendor", exporter.Name);
        Assert.Equal("https://vendor.example/v1/logs", exporter.Endpoint);
    }

    [Fact]
    public void Defaults_AddNoExportersAndExportEverything()
    {
        Assert.Empty(new OtelConfiguration().AdditionalLogExporters);

        var exporter = new OtlpLogExporterOptions();
        Assert.True(exporter.Enabled);
        Assert.True(exporter.IncludeMessageTemplate);
        Assert.Equal(LogEventLevel.Verbose, exporter.MinimumLevel);
        Assert.Empty(exporter.RequiredProperties);
        Assert.Null(exporter.Filter);
    }
}
