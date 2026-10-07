using Serilog.Core;
using Serilog.Events;

namespace EasyOpenTelemetry.Tests;

/// <summary>Test sink that records every event it receives, in order.</summary>
internal sealed class CollectingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = [];

    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}
