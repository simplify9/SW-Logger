using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace SW.Logger.Console;

/// <summary>
/// Extension methods for registering the SW Console Logger with the DI container.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers Serilog as the application logger with console output.
    /// <para>
    /// When a debugger is attached the output is plain text; otherwise it uses the
    /// compact JSON formatter suitable for log-aggregation pipelines.
    /// </para>
    /// <para>
    /// Settings are read from the <c>SwLogger</c> section of <c>appsettings.json</c> and
    /// can be overridden by the optional <paramref name="configure"/> callback.
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the logger to.</param>
    /// <param name="configure">
    /// Optional callback to configure <see cref="LoggerOptions"/> before the logger is built.
    /// Runs before the <c>appsettings.json</c> binding so that file settings take precedence.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
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
            //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", hostEnvironment.EnvironmentName)
            .Enrich.WithProperty("ApplicationVersion", loggerOptions.ApplicationVersion)
            .Enrich.WithProperty("Application", loggerOptions.ApplicationName);
        
        loggerConfiguration = Debugger.IsAttached
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());

        Log.Information("Serilog started from SwLogger.");

        services.AddSerilog(loggerConfiguration.CreateLogger());

        return services;
    }
}