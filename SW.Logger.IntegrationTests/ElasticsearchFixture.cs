using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace SW.Logger.IntegrationTests;

/// <summary>
/// A throwaway single-node Elasticsearch, pinned to the 8.x line the sink targets
/// (Elastic.Serilog.Sinks 8.18). Security is off so the tests can read it back over
/// plain HTTP; nothing here ever talks to a real cluster.
/// </summary>
public sealed class ElasticsearchFixture : IAsyncLifetime
{
    private const int Port = 9200;

    private readonly IContainer _container = new ContainerBuilder("elasticsearch:8.18.2")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
        .WithPortBinding(Port, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
            .ForPort(Port)
            .ForPath("/_cluster/health")))
        .Build();

    public string Url => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}";

    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri(Url) };
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await _container.DisposeAsync();
    }

    public async Task<JsonElement> Get(string path) =>
        await Client.GetFromJsonAsync<JsonElement>(path);

    /// <summary>Polls until <paramref name="condition"/> holds, or fails after <paramref name="timeout"/>.</summary>
    public static async Task Eventually(Func<Task<bool>> condition, TimeSpan timeout, string because)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail(because);
            await Task.Delay(500);
        }
    }
}
