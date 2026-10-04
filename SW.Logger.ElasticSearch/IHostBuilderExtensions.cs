using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

using System;
using System.Diagnostics;
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
using Serilog.Formatting.Compact;
using CertificateValidations = Elasticsearch.Net.CertificateValidations;

namespace SW.Logger.ElasticSerach
{
    public static class IHostBuilderExtensions
    {
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
                .Enrich.WithProperty("ApplicationVersion", loggerOptions.ApplicationVersion);

            loggerConfiguration = Debugger.IsAttached
                ? loggerConfiguration.WriteTo.Console()
                : loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());

            if (hostBuilderContext.HostingEnvironment.IsDevelopment())
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Debug();

            if (loggerOptions.ElasticsearchUrl != null && loggerOptions.ElasticsearchEnvironments != null &&
                loggerOptions.ElasticsearchEnvironments.Split(',').Select(env => env.Trim()).Contains(
                    hostBuilderContext.HostingEnvironment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
            {
                var dataStream = new DataStreamName("logs", loggerOptions.ApplicationName.ToLower(),
                    hostBuilderContext.HostingEnvironment.EnvironmentName);
                RetentionManager.Start(loggerOptions, dataStream.ToString());

                loggerConfiguration = loggerConfiguration
                    .WriteTo.Elasticsearch(new[] { new Uri(loggerOptions.ElasticsearchUrl) }, opts =>
                    {
                        opts.DataStream = dataStream;
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

    }
}