using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace SW.Logger.Console;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSWConsoleLogger(this IServiceCollection services,
        Action<LoggerOptions> configure = null)
    {
        var loggerOptions = new LoggerOptions
        {
            ApplicationVersion = Assembly.GetCallingAssembly().GetName().Version.ToString()
        };


        if (configure != null) configure.Invoke(loggerOptions);
        services.AddSingleton(loggerOptions);

        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        configuration.GetSection(LoggerOptions.ConfigurationSection).Bind(loggerOptions);

        var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is((LogEventLevel)loggerOptions.LoggingLevel)
            // Framework internals are noisy and slow at Debug/Information. Request logging
            // (UseSWConsoleLogger) replaces ASP.NET Core's own per-request lines.
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            // Quiet unless slow (see LoggerOptions.SlowQueryMilliseconds); failures always show.
            .MinimumLevel.Override(RequestLogging.EfCommandSource,
                loggerOptions.SlowQueryMilliseconds > 0 ? LogEventLevel.Information : LogEventLevel.Warning)
            // Logs "Request context set successfully" on every request.
            .MinimumLevel.Override("UseHttpUserRequestContext", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", hostEnvironment.EnvironmentName)
            .Enrich.WithProperty("ApplicationVersion", loggerOptions.ApplicationVersion)
            .Enrich.WithProperty("Application", loggerOptions.ApplicationName);
        
        loggerConfiguration = Debugger.IsAttached
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());

        if (loggerOptions.SlowQueryMilliseconds > 0)
            loggerConfiguration.Filter.ByExcluding(e =>
                RequestLogging.IsFastEfCommand(e, loggerOptions.SlowQueryMilliseconds));

        // The request-completed line is Information; keep it flowing when the global level is higher.
        if (loggerOptions.LogRequests)
            loggerConfiguration.MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware",
                (LogEventLevel)Math.Min(loggerOptions.LoggingLevel, (int)LogEventLevel.Information));

        Log.Information("Serilog started from SwLogger.");

        services.AddSerilog(loggerConfiguration.CreateLogger());

        return services;
    }
}