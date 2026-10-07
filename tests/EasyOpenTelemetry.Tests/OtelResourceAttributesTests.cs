using EasyOpenTelemetry.Configuration;

namespace EasyOpenTelemetry.Tests;

public class OtelResourceAttributesTests
{
    [Fact]
    public void From_MapsServiceNameAndEnvironment()
    {
        var attributes = OtelResourceAttributes.From(new OtelConfiguration
        {
            ServiceName = "my-service",
            Environment = "staging"
        });

        Assert.Equal("my-service", attributes["service.name"]);
        Assert.Equal("staging", attributes["deployment.environment"]);
    }

    [Fact]
    public void From_LetsAdditionalResourceAttributesWin()
    {
        var config = new OtelConfiguration { ServiceName = "my-service", Environment = "staging" };
        config.AdditionalResourceAttributes["service.name"] = "renamed";
        config.AdditionalResourceAttributes["service.version"] = "1.2.3";

        var attributes = OtelResourceAttributes.From(config);

        Assert.Equal("renamed", attributes["service.name"]);
        Assert.Equal("staging", attributes["deployment.environment"]);
        Assert.Equal("1.2.3", attributes["service.version"]);
    }
}
