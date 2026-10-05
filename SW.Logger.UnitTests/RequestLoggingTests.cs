using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Serilog.Events;
using SW.Logger.Console;

namespace SW.Logger.UnitTests;

public class RequestLoggingTests
{
    private static readonly string[] Quiet = RequestLogging.ParsePaths("/health, metrics");

    private static DefaultHttpContext Context(string path, int status = 200, string routePattern = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.StatusCode = status;
        if (routePattern != null)
            ctx.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse(routePattern), 0,
                EndpointMetadataCollection.Empty, null));
        return ctx;
    }

    [Theory]
    [InlineData("api/orders/{id}", "/api/orders/{id}")]
    [InlineData("/api/orders/{id}", "/api/orders/{id}")]
    public void Route_is_the_template_with_a_leading_slash(string pattern, string expected) =>
        Assert.Equal(expected, RequestLogging.GetRoute(Context("/api/orders/123", routePattern: pattern)));

    [Fact]
    public void Unmatched_requests_share_one_route() =>
        Assert.Equal("unmatched", RequestLogging.GetRoute(Context("/wp-admin/x.php", 404)));

    [Fact]
    public void Success_is_information() =>
        Assert.Equal(LogEventLevel.Information, RequestLogging.GetLevel(Context("/api/orders/1"), null, Quiet));

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    public void Probes_are_verbose(string path) =>
        Assert.Equal(LogEventLevel.Verbose, RequestLogging.GetLevel(Context(path), null, Quiet));

    [Fact]
    public void Probe_behind_a_path_base_is_verbose() =>
        Assert.Equal(LogEventLevel.Verbose,
            RequestLogging.GetLevel(Context("/rates/health", routePattern: "/health"), null, Quiet));

    [Fact]
    public void Failing_probe_is_still_an_error() =>
        Assert.Equal(LogEventLevel.Error, RequestLogging.GetLevel(Context("/health", 503), null, Quiet));

    [Fact]
    public void Exception_is_an_error() =>
        Assert.Equal(LogEventLevel.Error, RequestLogging.GetLevel(Context("/x"), new Exception(), Quiet));

    [Fact]
    public void Default_level_is_information() =>
        Assert.Equal((int)LogEventLevel.Information, new LoggerOptions().LoggingLevel);

    private static LogEvent Ef(LogEventLevel level, string elapsed, string source = RequestLogging.EfCommandSource) =>
        new(DateTimeOffset.UtcNow, level, null, new MessageTemplate("x", Array.Empty<Serilog.Parsing.MessageTemplateToken>()),
            new[]
            {
                new LogEventProperty("SourceContext", new ScalarValue(source)),
                new LogEventProperty("elapsed", new ScalarValue(elapsed))
            });

    [Fact]
    public void Fast_ef_command_is_excluded() =>
        Assert.True(RequestLogging.IsFastEfCommand(Ef(LogEventLevel.Information, "12"), 500));

    [Fact]
    public void Slow_ef_command_is_kept() =>
        Assert.False(RequestLogging.IsFastEfCommand(Ef(LogEventLevel.Information, "812"), 500));

    [Fact]
    public void Failed_ef_command_is_kept() =>
        Assert.False(RequestLogging.IsFastEfCommand(Ef(LogEventLevel.Error, "1"), 500));

    [Fact]
    public void Other_sources_are_untouched() =>
        Assert.False(RequestLogging.IsFastEfCommand(Ef(LogEventLevel.Information, "1", "Other"), 500));
}
