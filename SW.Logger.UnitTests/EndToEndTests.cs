using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using SW.Logger.Console;

namespace SW.Logger.UnitTests;

[Collection("console")]
public class EndToEndTests
{
    private static async Task<string> Run(int loggingLevel, params string[] paths) =>
        await Run(loggingLevel, null, paths);

    private static async Task<string> Run(int loggingLevel, Action<ILoggerFactory> act, params string[] paths) =>
        await Run(loggingLevel, act, null, paths);

    private static async Task<string> Run(int loggingLevel, Action<ILoggerFactory> act,
        Action<HttpClient> configureClient, params string[] paths)
    {
        var original = System.Console.Out;
        var captured = new StringWriter();
        System.Console.SetOut(captured);
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSWConsoleLogger(o =>
            {
                o.ApplicationName = "test";
                o.LoggingLevel = loggingLevel;
            });
            var app = builder.Build();
            // Stands in for authentication: a request with an X-Test-User header is signed in as that user.
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Headers.TryGetValue("X-Test-User", out var user))
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()) }, "test"));
                await next();
            });
            app.UseSWConsoleLogger();
            app.UseRouting();
            app.UseEndpoints(e => e.MapGet("/api/orders/{id}", (int id) => "ok"));
            await app.StartAsync();
            act?.Invoke(app.Services.GetRequiredService<ILoggerFactory>());
            var client = app.GetTestClient();
            configureClient?.Invoke(client);
            foreach (var p in paths) await client.GetAsync(p);
            await app.StopAsync();
        }
        finally
        {
            System.Console.SetOut(original);
        }

        return captured.ToString();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Request_line_carries_the_route_template(int level)
    {
        var output = await Run(level, "/api/orders/123", "/api/orders/456", "/nope", "/health");
        Assert.Contains("\"Route\":\"/api/orders/{id}\"", output);
        Assert.Contains("\"Route\":\"unmatched\"", output);
        Assert.DoesNotContain("\"RequestPath\":\"/health\"", output);
    }

    [Fact]
    public async Task Request_line_carries_the_caller()
    {
        var output = await Run(3, null, c =>
        {
            c.DefaultRequestHeaders.Add("X-Test-User", "customer-42");
            c.DefaultRequestHeaders.UserAgent.ParseAdd("RestSharp/110.2.0.0");
        }, "/api/orders/1");
        Assert.Contains("\"User\":\"customer-42\"", output);
        Assert.Contains("\"UserAgent\":\"RestSharp/110.2.0.0\"", output);
    }

    [Fact]
    public async Task Anonymous_request_line_has_no_user()
    {
        var output = await Run(3, "/api/orders/1");
        Assert.Contains("\"Route\":\"/api/orders/{id}\"", output);
        Assert.DoesNotContain("\"User\":", output);
    }

    [Fact]
    public async Task Framework_internals_are_quiet()
    {
        var output = await Run(2, "/api/orders/1");
        Assert.DoesNotContain("Microsoft.AspNetCore", output);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Only_slow_ef_commands_are_logged(int level)
    {
        var output = await Run(level, lf =>
        {
            var ef = lf.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");
            ef.LogInformation("Executed DbCommand ({elapsed}ms) [{sql}]", "12", "fast-query");
            ef.LogInformation("Executed DbCommand ({elapsed}ms) [{sql}]", "812", "slow-query");
            ef.LogError("Failed executing DbCommand ({elapsed}ms) [{sql}]", "3", "failed-query");
        });
        Assert.DoesNotContain("fast-query", output);
        Assert.Contains("slow-query", output);
        Assert.Contains("failed-query", output);
    }
}
