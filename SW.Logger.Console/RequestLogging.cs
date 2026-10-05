using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace SW.Logger.Console;

internal static class RequestLogging
{
    public const string RouteProperty = "Route";
    public const string UnmatchedRoute = "unmatched";

    /// <summary>
    /// The route template ("/api/orders/{id}") rather than the concrete path, so latency and
    /// error rates can be grouped per endpoint instead of per id. Requests that matched no
    /// endpoint share one value, so scanners hitting random paths don't create unbounded series.
    /// </summary>
    public static string GetRoute(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint() is not RouteEndpoint { RoutePattern.RawText: { Length: > 0 } raw })
            return UnmatchedRoute;
        return raw.StartsWith('/') ? raw : "/" + raw;
    }

    public static LogEventLevel GetLevel(HttpContext httpContext, Exception? ex, string[] quietPaths)
    {
        if (ex != null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        // The route template is matched too: with UsePathBase("/svc") the request path seen here
        // is "/svc/health", but the endpoint's template is "/health".
        var route = GetRoute(httpContext);
        foreach (var prefix in quietPaths)
            if (httpContext.Request.Path.StartsWithSegments(prefix) ||
                (route != UnmatchedRoute && new PathString(route).StartsWithSegments(prefix)))
                return LogEventLevel.Verbose;

        return LogEventLevel.Information;
    }

    public static string[] ParsePaths(string paths) =>
        (paths ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p => p.StartsWith('/') ? p : "/" + p)
        .ToArray();

    public static void Configure(RequestLoggingOptions options, LoggerOptions loggerOptions)
    {
        var quietPaths = ParsePaths(loggerOptions.QuietRequestPaths);
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            diagnosticContext.Set(RouteProperty, GetRoute(httpContext));
        options.GetLevel = (httpContext, _, ex) => GetLevel(httpContext, ex, quietPaths);
    }

    public const string EfCommandSource = "Microsoft.EntityFrameworkCore.Database.Command";

    /// <summary>
    /// True for a successful EF command event that finished faster than the slow-query threshold.
    /// Warnings and errors (failed commands) are never excluded.
    /// </summary>
    public static bool IsFastEfCommand(LogEvent e, int thresholdMs)
    {
        if (e.Level >= LogEventLevel.Warning) return false;
        if (!e.Properties.TryGetValue("SourceContext", out var source) ||
            source is not ScalarValue { Value: EfCommandSource }) return false;
        if (!e.Properties.TryGetValue("elapsed", out var elapsed) || elapsed is not ScalarValue sv) return false;
        return double.TryParse(Convert.ToString(sv.Value, System.Globalization.CultureInfo.InvariantCulture),
                   System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                   out var ms) && ms < thresholdMs;
    }
}
