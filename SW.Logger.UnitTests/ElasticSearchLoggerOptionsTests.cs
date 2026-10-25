using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using SW.Logger.ElasticSerach;
using Xunit;

namespace SW.Logger.UnitTests;

public class ElasticSearchLoggerOptionsTests
{
    [Fact]
    public void RetentionPolicy_WithoutReplicasAfterRollover_HasNoWarmPhase()
    {
        using var policy = JsonDocument.Parse(RetentionManager.PolicyBody(90));
        var phases = policy.RootElement.GetProperty("policy").GetProperty("phases");

        Assert.False(phases.TryGetProperty("warm", out _));
        Assert.Equal("90d", phases.GetProperty("delete").GetProperty("min_age").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void RetentionPolicy_WithReplicasAfterRollover_DropsReplicasRightAfterRollover(int replicas)
    {
        using var policy = JsonDocument.Parse(RetentionManager.PolicyBody(90, replicas));
        var phases = policy.RootElement.GetProperty("policy").GetProperty("phases");
        var warm = phases.GetProperty("warm");

        Assert.Equal("0ms", warm.GetProperty("min_age").GetString());
        Assert.Equal(replicas, warm.GetProperty("actions").GetProperty("allocate").GetProperty("number_of_replicas").GetInt32());
        // These clusters have no warm-tier nodes; the index must stay where it is.
        Assert.False(warm.GetProperty("actions").GetProperty("migrate").GetProperty("enabled").GetBoolean());
        Assert.True(phases.TryGetProperty("hot", out _));
        Assert.True(phases.TryGetProperty("delete", out _));
    }

    [Fact]
    public void RetentionPolicy_NegativeReplicas_IsIgnored()
    {
        using var policy = JsonDocument.Parse(RetentionManager.PolicyBody(90, -1));
        Assert.False(policy.RootElement.GetProperty("policy").GetProperty("phases").TryGetProperty("warm", out _));
    }

    private sealed class ListSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();
        public void Emit(LogEvent logEvent) { lock (Events) Events.Add(logEvent); }
    }

    // No ElasticsearchUrl, so only the overrides and console sink are configured; the test sink
    // added through configureLogger sees exactly what would be shipped.
    [Fact]
    public void MinimumLevelOverrides_FromConfiguration_FilterThatSourceOnly()
    {
        var sink = new ListSink();
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SwLogger:ApplicationName"] = "test",
                ["SwLogger:LoggingLevel"] = "2",
                ["SwLogger:MinimumLevelOverrides:Microsoft.EntityFrameworkCore"] = "3",
            }))
            .UseSwElasticSearchLogger(configureLogger: (_, l) => l.WriteTo.Sink(sink))
            .Build();

        var factory = host.Services.GetRequiredService<ILoggerFactory>();
        factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").LogInformation("Executed DbCommand");
        factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").LogWarning("slow query");
        factory.CreateLogger("MyApp.Orders").LogInformation("order created");

        var messages = sink.Events.Select(e => e.MessageTemplate.Text).ToList();
        Assert.DoesNotContain("Executed DbCommand", messages);
        Assert.Contains("slow query", messages);
        Assert.Contains("order created", messages);
    }
}
