using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SW.Logger.ElasticSerach;
using Xunit;

namespace SW.Logger.IntegrationTests;

/// <summary>
/// What a service relies on SimplyWorks.Logger.ElasticSearch for, against a real
/// (throwaway) Elasticsearch: its logs land in its data stream, and the retention
/// policy it is configured with is saved and applied to that stream.
/// </summary>
[Trait("Category", "CriticalJourney")]
public class ElasticSearchLoggerJourneyTests : IClassFixture<ElasticsearchFixture>
{
    private readonly ElasticsearchFixture _elastic;

    public ElasticSearchLoggerJourneyTests(ElasticsearchFixture elastic) => _elastic = elastic;

    [Fact]
    public async Task Logs_ReachTheDataStream_AtTheConfiguredLevel()
    {
        const string app = "journeylogs";
        var marker = Guid.NewGuid().ToString("N");

        using (var host = BuildHost(app, deleteAfterDays: 30))
        {
            await host.StartAsync();
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Journey");
            logger.LogInformation("Order {Marker} shipped", marker);
            logger.LogDebug("Debug {Marker} below the configured level", marker);
            await host.StopAsync();
        } // Disposing the host flushes the sink's buffer.

        var stream = $"logs-{app}-development";
        JsonElement hits = default;
        await ElasticsearchFixture.Eventually(async () =>
        {
            await _elastic.Client.PostAsync($"{stream}/_refresh", null);
            var search = await _elastic.Client.GetAsync($"{stream}/_search?q={marker}&size=10");
            if (!search.IsSuccessStatusCode)
                return false;
            hits = (await search.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hits").GetProperty("hits");
            return hits.GetArrayLength() > 0;
        }, TimeSpan.FromSeconds(60), $"The log never reached {stream}.");

        // Only the Information entry: the Debug one is below the configured level.
        var log = Assert.Single(hits.EnumerateArray()).GetProperty("_source");
        Assert.Equal($"Order \"{marker}\" shipped", log.GetProperty("message").GetString());
        Assert.Equal("Information", log.GetProperty("log.level").GetString());
        Assert.Equal("Development", log.GetProperty("labels").GetProperty("Environment").GetString());
        Assert.Equal(marker, log.GetProperty("labels").GetProperty("Marker").GetString());
    }

    [Fact]
    public async Task RetentionPolicy_IsSavedAndAppliedToTheDataStream()
    {
        const string app = "journeyretention";
        var stream = $"logs-{app}-development";
        var policy = $"{stream}-retention";

        // The policy can only be applied to a stream that exists (it is retried daily
        // otherwise); create it the way the built-in logs-*-* template would.
        var created = await _elastic.Client.PutAsync($"_data_stream/{stream}", null);
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());

        using var host = BuildHost(app, deleteAfterDays: 14, replicasAfterRollover: 0);
        await host.StartAsync();

        await ElasticsearchFixture.Eventually(async () =>
        {
            var settings = await _elastic.Client.GetAsync($"{stream}/_settings/index.lifecycle.name");
            return settings.IsSuccessStatusCode &&
                   (await settings.Content.ReadAsStringAsync()).Contains($"\"{policy}\"");
        }, TimeSpan.FromSeconds(60), $"{policy} was never applied to {stream}.");

        var saved = await _elastic.Get($"_ilm/policy/{policy}");
        var phases = saved.GetProperty(policy).GetProperty("policy").GetProperty("phases");
        Assert.Equal("14d", phases.GetProperty("delete").GetProperty("min_age").GetString());
        Assert.Equal(0, phases.GetProperty("warm").GetProperty("actions").GetProperty("allocate")
            .GetProperty("number_of_replicas").GetInt32());

        await host.StopAsync();
    }

    private IHost BuildHost(string app, int deleteAfterDays, int? replicasAfterRollover = null) =>
        Host.CreateDefaultBuilder()
            .UseEnvironment("Development")
            .UseSwElasticSearchLogger(o =>
            {
                o.ApplicationName = app;
                o.LoggingLevel = 2; // Information
                o.ElasticsearchUrl = _elastic.Url;
                o.ElasticsearchUser = "test-user";
                o.ElasticsearchPassword = "not-a-real-password";
                o.ElasticsearchDeleteIndexAfterDays = deleteAfterDays;
                o.ElasticsearchReplicasAfterRollover = replicasAfterRollover;
            })
            .Build();
}
