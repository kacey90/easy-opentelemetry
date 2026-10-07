namespace EasyOpenTelemetry.Configuration;

/// <summary>
/// Single source of truth for the resource attributes attached to traces, metrics and every log sink.
/// </summary>
internal static class OtelResourceAttributes
{
    /// <summary>
    /// Builds the resource attributes for <paramref name="configuration"/>. Entries in
    /// <see cref="OtelConfiguration.AdditionalResourceAttributes"/> win on key collisions, so callers can
    /// override <c>deployment.environment</c> or <c>service.name</c>.
    /// </summary>
    public static Dictionary<string, object> From(OtelConfiguration configuration)
    {
        var attributes = new Dictionary<string, object>
        {
            ["deployment.environment"] = configuration.Environment,
            ["service.name"] = configuration.ServiceName
        };

        foreach (var (key, value) in configuration.AdditionalResourceAttributes)
        {
            attributes[key] = value;
        }

        return attributes;
    }
}
