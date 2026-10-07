using EasyOpenTelemetry.Configuration;
using Microsoft.Extensions.Logging;

namespace EasyOpenTelemetry.Logging;

public static class EventLoggerExtensions
{
    public const string EventNameAttribute = "event.name"; // OTel semantic convention

    public static void LogEvent(
        this ILogger logger, 
        string eventName, 
        IEnumerable<KeyValuePair<string, object?>>? properties = null,
        LogLevel level = LogLevel.Information)
    {
        if (!logger.IsEnabled(level)) return;

        var state = new List<KeyValuePair<string, object?>> { new(EventNameAttribute, eventName) };
        if (properties is not null) state.AddRange(properties);

        // Providers (Serilog, OpenTelemetry) read this as the message template. Without it Serilog falls back to
        // a "{State:l}" template plus a synthetic "State" property on every event.
        state.Add(new("{OriginalFormat}", "Event " + eventName.Replace("{", "{{").Replace("}", "}}")));

        logger.Log(level, default, state, null, static (s, _) => $"Event {s[0].Value}");
    }
}