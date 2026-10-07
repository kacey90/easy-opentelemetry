# EasyOpenTelemetry

A small, focused **NuGet class library** that adds OpenTelemetry observability (tracing, metrics, logging) to .NET applications with minimal configuration. The whole value proposition is *simplicity* — keep the public API tiny and the defaults sensible.

## Project Type & Stack

- **Type:** Reusable class library / NuGet package (`GeneratePackageOnBuild=true`)
- **Target framework:** `net10.0`
- **Language:** C# 14 (`ImplicitUsings` + `Nullable` enabled, set in `Directory.Build.props`)
- **Maintainer model:** Solo maintainer / open-source
- **Package versions:** Centrally managed via `Directory.Packages.props` (CPM). Add new packages there with `<PackageVersion>`; csproj `<PackageReference>` entries carry **no** `Version` attribute.
- **Core dependencies:** OpenTelemetry SDK 1.15.x (+ AspNetCore, Http, Runtime, Process[beta] instrumentation, Prometheus.AspNetCore[beta] exporter), Serilog (`Serilog.Extensions.Hosting`, `Serilog.Sinks.OpenTelemetry`). DI/Hosting abstractions come from the ASP.NET Core shared framework (`<FrameworkReference Include="Microsoft.AspNetCore.App" />`), which the library requires.

## Solution Layout

```
easy-opentelemetry/
├── EasyOpenTelemetry.sln
├── global.json                     # Pins SDK 10.0.x (rollForward: latestMinor)
├── Directory.Build.props           # Shared props: TFM, Nullable, ImplicitUsings, LangVersion, analyzers
├── Directory.Packages.props        # Central Package Management — all package versions live here
├── README.md                       # Packed into the NuGet package (PackageReadmeFile)
├── EasyOpenTelemetry/              # The library
│   ├── OtelConfigurationBuilder.cs # Static entry: Create(...) / FromEnvironment(...)
│   └── Configuration/
│       ├── OtelConfiguration.cs                  # Options object (all knobs live here)
│       ├── OtlpLogExporterOptions.cs             # Per-exporter options for AdditionalLogExporters
│       ├── OtelResourceAttributes.cs             # Shared resource-attribute dictionary (internal)
│       ├── OtelServiceCollectionExtensions.cs    # AddEasyOpenTelemetry(...) overloads
│       ├── SerilogHostBuilderExtensions.cs       # UseEasyOpenTelemetryWithSerilog(...)
│       └── PrometheusApplicationBuilderExtensions.cs # UseEasyOpenTelemetryPrometheus(...)
└── tests/
    └── EasyOpenTelemetry.Tests/    # xUnit
```

## Architecture Notes

This is a **library, not an application**, so app-level architectures (VSA, Clean, DDD, Modular Monolith) do not apply. The relevant discipline is **public API design**:

- The public surface is intentionally minimal: `OtelConfigurationBuilder`, `OtelConfiguration`, and the two extension method families. Treat every `public` symbol as an API contract — adding one is a commitment, removing/renaming one is a breaking change.
- Configuration flows one way: caller builds an `OtelConfiguration` (directly, via `OtelConfigurationBuilder`, or via an `Action<OtelConfiguration>`), and the extension methods consume it. New options should be added as properties on `OtelConfiguration` with sensible defaults, not as new method parameters.
- Keep instrumentation toggles as `bool Enable*` properties (existing convention) so callers can opt out selectively.
- Resource attributes are centralized in the internal `OtelResourceAttributes.From(...)` — every sink/provider (traces, metrics, primary and additional Serilog sinks) must use it rather than duplicating the `deployment.environment` / `service.name` mapping. `AdditionalResourceAttributes` win on key collisions.
- Additional log exporters are Serilog sub-loggers built by the internal `AdditionalLogExporter(...)` seam (filters in an outer sub-logger, enrichers in a nested one, because Serilog runs enrichers before filters). They pass `ignoreEnvironment: true` so `OTEL_EXPORTER_OTLP_*` can't redirect them.

## Conventions

- **Namespaces:** `EasyOpenTelemetry` for entry points, `EasyOpenTelemetry.Configuration` for the config/extension types. File-scoped namespaces.
- **Extension methods:** static classes named `<Thing>Extensions`; return the builder/`IServiceCollection`/`IHostBuilder` to keep chaining fluent.
- **Defaults:** every `OtelConfiguration` property has a default that produces a working local setup (OTLP gRPC → `http://localhost:4317`). Don't introduce required-but-unset options.
- **Validation:** fail fast with `ArgumentException` for required values (see `ServiceName` check). Validate in the canonical `AddEasyOpenTelemetry(IServiceCollection, OtelConfiguration)` overload that all others funnel into.
- **No new dependencies lightly:** this package is "easy" because it's a thin layer. Prefer the existing OpenTelemetry/Serilog packages over pulling in new ones.

## Testing

- Framework: **xUnit** in `tests/EasyOpenTelemetry.Tests` (`IsPackable=false`).
- Focus tests on the public contract: configuration mapping, validation/throwing behavior, default fallbacks (`FromEnvironment`), and that `AddEasyOpenTelemetry` produces a buildable `ServiceProvider`.
- Avoid asserting on OpenTelemetry SDK internals; assert on observable behavior of *our* surface.
- Run: `dotnet test`

## Build, Pack & Publish (manual, solo)

- **Build:** `dotnet build` (or `dotnet build -c Release`)
- **Test:** `dotnet test`
- **Pack:** `dotnet pack -c Release` — the package is produced automatically on build (`GeneratePackageOnBuild`); `README.md` is included via the `<None Include="..\README.md" Pack="true">` item.
- **Version:** bump `<Version>` in `EasyOpenTelemetry/EasyOpenTelemetry.csproj` (SemVer). A `public` API removal/rename is a major bump; additive options are a minor bump.
- **Publish:** `dotnet nuget push bin/Release/EasyOpenTelemetry.<version>.nupkg --source https://api.nuget.org/v3/index.json --api-key <KEY>`
- Keep the README's "Configuration Options" table and the `OtelConfiguration` properties in sync — the README ships inside the package.

> CI is documented as a convention only (no workflow committed yet). When automating, the natural pipeline is: restore → build → test → pack → push-on-tag.

## Working Agreements for Claude

- This is a public NuGet library: be conservative with the public API and call out any breaking change explicitly, with a suggested version bump.
- When adding a config option, also: give it a default, wire it through the relevant `Configure*`/extension method, and update both the README table and (ideally) a test.
- Use the Roslyn MCP navigator tools (provided by the `dotnet-claude-kit` plugin) for reference/impact analysis before refactoring public symbols.
- Match existing style: file-scoped namespaces, fluent returns, `Enable*` toggles, fail-fast validation.
