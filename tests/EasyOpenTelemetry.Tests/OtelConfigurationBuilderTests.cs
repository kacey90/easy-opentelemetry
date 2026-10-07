using EasyOpenTelemetry;

namespace EasyOpenTelemetry.Tests;

public class OtelConfigurationBuilderTests
{
    [Fact]
    public void Create_SetsProvidedValues()
    {
        var config = OtelConfigurationBuilder.Create(
            serviceName: "my-service",
            environment: "production",
            otlpEndpoint: "http://localhost:4317");

        Assert.Equal("my-service", config.ServiceName);
        Assert.Equal("production", config.Environment);
        Assert.Equal("http://localhost:4317", config.OtlpEndpoint);
    }

    [Fact]
    public void FromEnvironment_FallsBackToDefaults_WhenNoEnvironmentVariablesSet()
    {
        var config = OtelConfigurationBuilder.FromEnvironment("explicit-service");

        Assert.Equal("explicit-service", config.ServiceName);
        Assert.False(string.IsNullOrWhiteSpace(config.Environment));
        Assert.False(string.IsNullOrWhiteSpace(config.OtlpEndpoint));
    }
}
