using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SW.Logger.OpenTelemetry;

/// <summary>
/// Extension methods for registering the SW OpenTelemetry Logger with the DI container.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry logging, tracing, and metrics to the service collection.
    /// Configuration is read from the "SwLogger" section; individual settings can be
    /// overridden via the <paramref name="configure"/> callback.
    ///
    /// Supports dual-export (OTLP + Azure Monitor / Application Insights simultaneously)
    /// which is the recommended migration path away from the App Insights SDK.
    /// </summary>
    public static IServiceCollection AddSWOpenTelemetryLogger(
        this IServiceCollection services,
        Action<LoggerOptions>? configure = null)
    {
        var loggerOptions = new LoggerOptions
        {
            ApplicationVersion = Assembly.GetCallingAssembly().GetName().Version?.ToString() ?? "0.0.0"
        };

        if (configure != null) configure.Invoke(loggerOptions);

        services.AddSingleton(loggerOptions);

        var sp = services.BuildServiceProvider();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();

        configuration.GetSection(LoggerOptions.ConfigurationSection).Bind(loggerOptions);

        var isOtlpEnvironment = loggerOptions.OtlpExportEnvironments
            .Split(',')
            .Select(e => e.Trim())
            .Contains(hostEnvironment.EnvironmentName, StringComparer.OrdinalIgnoreCase);

        var hasAzureMonitor = !string.IsNullOrWhiteSpace(loggerOptions.AzureMonitorConnectionString);

        // ── Shared resource ───────────────────────────────────────────────────
        void ConfigureResource(ResourceBuilder r)
        {
            r.AddService(
                serviceName: loggerOptions.ApplicationName,
                serviceVersion: loggerOptions.ApplicationVersion,
                serviceInstanceId: loggerOptions.ServiceInstanceId ?? Environment.MachineName);

            r.AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = hostEnvironment.EnvironmentName
            });

            if (loggerOptions.AdditionalResourceAttributes.Count > 0)
                r.AddAttributes(loggerOptions.AdditionalResourceAttributes
                    .ToDictionary(kv => kv.Key, kv => (object)kv.Value));
        }

        // ── Helper: apply OTLP exporter options ───────────────────────────────
        void ApplyOtlpOptions(OtlpExporterOptions otlp)
        {
            if (!string.IsNullOrWhiteSpace(loggerOptions.OtlpEndpoint))
                otlp.Endpoint = new Uri(loggerOptions.OtlpEndpoint);

            otlp.Protocol = loggerOptions.OtlpProtocol == OtlpProtocolType.HttpProtobuf
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;

            foreach (var header in loggerOptions.OtlpHeaders)
                otlp.Headers += (string.IsNullOrEmpty(otlp.Headers) ? "" : ",") + $"{header.Key}={header.Value}";
        }

        // ── Logging ───────────────────────────────────────────────────────────
        if (loggerOptions.EnableLogging)
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(otelLogging =>
                {
                    otelLogging.SetResourceBuilder(ResourceBuilder.CreateDefault().Also(ConfigureResource));
                    otelLogging.IncludeFormattedMessage = true;
                    otelLogging.IncludeScopes = true;

                    if (isOtlpEnvironment)
                        otelLogging.AddOtlpExporter(ApplyOtlpOptions);
                    else
                        otelLogging.AddConsoleExporter();

                    if (hasAzureMonitor)
                        otelLogging.AddAzureMonitorLogExporter(o =>
                            o.ConnectionString = loggerOptions.AzureMonitorConnectionString);
                });
            });
        }

        // ── Tracing & Metrics ─────────────────────────────────────────────────
        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource);

        if (loggerOptions.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts => { opts.RecordException = true; })
                    .AddHttpClientInstrumentation();

                // Sampling: 1.0 = always-on (default), <1.0 = probabilistic
                if (loggerOptions.SamplingRatio < 1.0)
                    tracing.SetSampler(new TraceIdRatioBasedSampler(loggerOptions.SamplingRatio));

                if (isOtlpEnvironment)
                    tracing.AddOtlpExporter(ApplyOtlpOptions);
                else
                    tracing.AddConsoleExporter();

                if (hasAzureMonitor)
                    tracing.AddAzureMonitorTraceExporter(o =>
                        o.ConnectionString = loggerOptions.AzureMonitorConnectionString);
            });
        }

        if (loggerOptions.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (isOtlpEnvironment)
                    metrics.AddOtlpExporter(ApplyOtlpOptions);
                else
                    metrics.AddConsoleExporter();

                if (hasAzureMonitor)
                    metrics.AddAzureMonitorMetricExporter(o =>
                        o.ConnectionString = loggerOptions.AzureMonitorConnectionString);
            });
        }

        return services;
    }
}

internal static class ResourceBuilderExtensions
{
    internal static ResourceBuilder Also(this ResourceBuilder builder, Action<ResourceBuilder> configure)
    {
        configure(builder);
        return builder;
    }
}
