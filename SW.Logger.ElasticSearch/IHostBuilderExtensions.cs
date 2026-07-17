using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Elasticsearch.Net;
using Nest;
using CertificateValidations = Elasticsearch.Net.CertificateValidations;

namespace SW.Logger.ElasticSerach
{
    /// <summary>
    /// Extension methods for configuring the SW ElasticSearch Logger on the application host.
    /// </summary>
    public static class IHostBuilderExtensions
    {
        private const string IndexLifecycleName = "index.lifecycle.name";

        /// <summary>
        /// Configures Serilog on the host with structured logging to both the console and an
        /// ElasticSearch data stream.
        /// <para>
        /// Settings are read from the <c>SwLogger</c> section of <c>appsettings.json</c> and can
        /// be overridden by the optional <paramref name="configure"/> callback.
        /// ElasticSearch export is only activated when <see cref="LoggerOptions.ElasticsearchUrl"/>
        /// is set and the current environment is listed in
        /// <see cref="LoggerOptions.ElasticsearchEnvironments"/>.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">
        /// Optional callback to configure <see cref="LoggerOptions"/> before the logger is built.
        /// </param>
        /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder UseSwElasticSearchLogger(this IHostBuilder builder, Action<LoggerOptions> configure = null)
        {
            var loggerOptions = new LoggerOptions
            {
                ApplicationVersion = Assembly.GetCallingAssembly().GetName().Version.ToString()
            };

            if (configure != null) configure.Invoke(loggerOptions);

            return builder.UseSerilog((hostBuilderContext, loggerConfiguration) =>
                ConfigureSerilog(hostBuilderContext, loggerConfiguration, loggerOptions));
        }

        private static LoggerConfiguration ConfigureSerilog(HostBuilderContext hostBuilderContext,
            LoggerConfiguration loggerConfiguration, LoggerOptions loggerOptions)
        {
            hostBuilderContext.Configuration.GetSection(LoggerOptions.ConfigurationSection).Bind(loggerOptions);

            if (!string.IsNullOrEmpty(loggerOptions.ApplicationName))
            {
                var (isValid, validationError) = loggerOptions.ApplicationName.IsValidIndexName();
                if (!isValid)
                    throw new SWLoggerConfigurationException(validationError);
            }

            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Is((LogEventLevel)loggerOptions.LoggingLevel)
                //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", hostBuilderContext.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("ApplicationVersion", loggerOptions.ApplicationVersion)
                .WriteTo.Console();

            if (hostBuilderContext.HostingEnvironment.IsDevelopment())
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Debug();

            if (loggerOptions.ElasticsearchUrl != null && loggerOptions.ElasticsearchEnvironments != null &&
                loggerOptions.ElasticsearchEnvironments.Split(',').Select(env => env.Trim()).Contains(
                    hostBuilderContext.HostingEnvironment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
            {
                CreateLifeCyclePolicy(loggerOptions);
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Elasticsearch(new[] { new Uri(loggerOptions.ElasticsearchUrl) }, opts =>
                    {
                        opts.DataStream = new DataStreamName("logs", loggerOptions.ApplicationName.ToLower(), hostBuilderContext.HostingEnvironment.EnvironmentName);
                        opts.BootstrapMethod = BootstrapMethod.Failure;
                        opts.ConfigureChannel = channelOpts =>
                        {
                            channelOpts.BufferOptions = new BufferOptions
                            {

                            };
                        };
                    }, transport =>
                    {
                        if (string.IsNullOrWhiteSpace(loggerOptions.ElasticsearchCertificatePath))
                        {
                            transport.Authentication(new BasicAuthentication(loggerOptions.ElasticsearchUser, loggerOptions.ElasticsearchPassword));
                            transport.ServerCertificateValidationCallback((_, _, _, _) => true);
                        }
                        else
                        {
                            transport.Authentication(new BasicAuthentication(loggerOptions.ElasticsearchUser, loggerOptions.ElasticsearchPassword));
                            transport.ServerCertificateValidationCallback(
                                CertificateValidations.AuthorityIsRoot(
                                    new X509Certificate(loggerOptions.ElasticsearchCertificatePath)
                                )
                            );
                        }
                    });
            }

            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            Log.Information("Serilog started from SwLogger.");

            return loggerConfiguration; //.ReadFrom.Configuration(hostBuilderContext.Configuration);
        }

        private static void CreateLifeCyclePolicy(LoggerOptions loggerOptions)
        {
            var uri = new Uri(loggerOptions.ElasticsearchUrl);
            var settings = new ConnectionSettings(uri);
            settings.BasicAuthentication(loggerOptions.ElasticsearchUser, loggerOptions.ElasticsearchPassword);
            var client = new ElasticClient(settings);

            // create lifecycle
            client.IndexLifecycleManagement.PutLifecycle(loggerOptions.GetPolicyName(), p =>
                p.Policy(po => po.Phases(a => a.Delete(w => w
                        .MinimumAge($"{loggerOptions.ElasticsearchDeleteIndexAfterDays}d")
                        .Actions(ac => ac
                            .Delete(f => f)
                        )
                    )
                )));

            // apply lifecycle on existing if any
            client.Indices.UpdateSettings(
                new UpdateIndexSettingsRequest($"{loggerOptions.ApplicationName.ToLower()}-*")
                {
                    IndexSettings = new IndexSettings
                    {
                        { IndexLifecycleName, loggerOptions.GetPolicyName() }
                    }
                });
        }

        private static string GetPolicyName(this LoggerOptions loggerOptions) =>
            $"{loggerOptions.ApplicationName.ToLower()}-policy";
    }
}