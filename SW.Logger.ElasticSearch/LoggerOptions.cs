using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SW.Logger.ElasticSerach
{
    public class LoggerOptions
    {
        public const string ConfigurationSection = "SwLogger";

        public LoggerOptions()
        {
            ElasticsearchEnvironments = "Development,Staging,Production";
            LoggingLevel = 3;
            ApplicationName = "unknownapp";
            ElasticsearchDeleteIndexAfterDays = 90;
        }

        public int LoggingLevel { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public string ElasticsearchUrl { get; set; }
        public string ElasticsearchUser { get; set; }
        public string ElasticsearchPassword { get; set; }
        public string ElasticsearchEnvironments { get; set; }
        
        public string ElasticsearchCertificatePath { get; set; }
        // Logs older than this are deleted from the data stream. 0 or less keeps them forever.
        public int ElasticsearchDeleteIndexAfterDays { get; set; }

        // Replica count for a month's backing index once it has rolled over (no longer written to).
        // 0 halves the disk older logs take; the index still being written keeps its replicas, so a
        // node restart never stops logging. Unset leaves replicas unchanged.
        public int? ElasticsearchReplicasAfterRollover { get; set; }

        // Minimum level per log source, e.g. { "Microsoft.EntityFrameworkCore": 3 } logs EF Core at
        // Warning and above only (Serilog levels: 0 Verbose, 1 Debug, 2 Information, 3 Warning,
        // 4 Error, 5 Fatal). Overrides LoggingLevel for that source and everything under it.
        public Dictionary<string, int> MinimumLevelOverrides { get; set; } = new();

    }
}
