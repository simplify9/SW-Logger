using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SW.Logger.ElasticSerach
{
    /// <summary>
    /// Configuration options for the SW ElasticSearch Logger.
    /// Bind from the <c>SwLogger</c> section of <c>appsettings.json</c> or supply via the
    /// <c>configure</c> callback on <see cref="IHostBuilderExtensions.UseSwElasticSearchLogger"/>.
    /// </summary>
    public class LoggerOptions
    {
        /// <summary>The configuration section key used to bind these options: <c>"SwLogger"</c>.</summary>
        public const string ConfigurationSection = "SwLogger";

        /// <summary>Initialises a new instance with sensible defaults.</summary>
        public LoggerOptions()
        {
            ElasticsearchEnvironments = "Development,Staging,Production";
            LoggingLevel = 3;
            ApplicationName = "unknownapp";
            ElasticsearchDeleteIndexAfterDays = 90;
        }

        /// <summary>
        /// Serilog minimum log level as an integer.
        /// Maps to <see cref="Serilog.Events.LogEventLevel"/>:
        /// 0 = Verbose, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Fatal.
        /// Default: <c>3</c> (Warning).
        /// </summary>
        public int LoggingLevel { get; set; }

        /// <summary>
        /// Application name used as the ElasticSearch data-stream name segment.
        /// Must be a valid ElasticSearch index name (lowercase, no special characters).
        /// Default: <c>"unknownapp"</c>.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Application version enriched on every log entry (<c>ApplicationVersion</c> property).
        /// Auto-populated from the calling assembly when not set explicitly.
        /// </summary>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// The URL of the ElasticSearch cluster (e.g. <c>https://localhost:9200</c>).
        /// When <see langword="null"/> ElasticSearch logging is skipped.
        /// </summary>
        public string ElasticsearchUrl { get; set; }

        /// <summary>Username for ElasticSearch basic authentication.</summary>
        public string ElasticsearchUser { get; set; }

        /// <summary>Password for ElasticSearch basic authentication.</summary>
        public string ElasticsearchPassword { get; set; }

        /// <summary>
        /// Comma-separated list of environment names in which ElasticSearch logging is active.
        /// Default: <c>"Development,Staging,Production"</c>.
        /// </summary>
        public string ElasticsearchEnvironments { get; set; }

        /// <summary>
        /// Absolute path to a CA certificate file used to validate the ElasticSearch TLS connection.
        /// When <see langword="null"/> or empty, certificate validation is disabled (all certs trusted).
        /// </summary>
        public string ElasticsearchCertificatePath { get; set; }

        /// <summary>
        /// Number of days after which ElasticSearch indices are deleted by the ILM policy.
        /// Default: <c>90</c>.
        /// </summary>
        public int ElasticsearchDeleteIndexAfterDays { get; set; }
    }
}
