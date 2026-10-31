using Microsoft.Extensions.Configuration;
using SW.Logger.Console;
using Xunit;

namespace SW.Logger.UnitTests;

public class LoggerOptionsBindingTests
{
    [Fact]
    public void Properties_bind_from_configuration_over_code()
    {
        var options = new LoggerOptions();
        options.Properties["Deployment"] = "from-code";
        options.Properties["Region"] = "eu";
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["SwLogger:Properties:Deployment"] = "acme" })
            .Build()
            .GetSection(LoggerOptions.ConfigurationSection).Bind(options);

        Assert.Equal("acme", options.Properties["Deployment"]);
        Assert.Equal("eu", options.Properties["Region"]);
    }
}
