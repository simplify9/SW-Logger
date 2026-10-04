using System;
using System.Collections.Concurrent;
using System.Threading;
using Elasticsearch.Net;
using Nest;

namespace SW.Logger.ElasticSerach
{
    // Applies the log retention policy directly to the data stream the logger writes to.
    //
    // It used to attach the policy to "{app}-*" indices, but logs go to the data stream
    // "logs-{app}-{env}", so the policy never applied and logs were kept forever. Setting the
    // policy on the data stream (not through an index template) also means template priority
    // battles between Elastic library versions can't undo it.
    //
    // Re-applied every 24 hours, because each monthly rollover creates its backing index from
    // whichever template wins, which may carry a different policy.
    internal static class RetentionManager
    {
        private static readonly TimeSpan ReapplyInterval = TimeSpan.FromHours(24);

        // One timer per data stream, kept alive for the life of the process.
        private static readonly ConcurrentDictionary<string, Timer> Timers = new();

        public static string PolicyName(string dataStream) => $"{dataStream}-retention";

        public static void Start(LoggerOptions options, string dataStream)
        {
            if (options.ElasticsearchDeleteIndexAfterDays <= 0)
            {
                Console.WriteLine($"SwLogger: log retention disabled for {dataStream} (ElasticsearchDeleteIndexAfterDays <= 0).");
                return;
            }

            // First run happens in the background so an unreachable cluster doesn't delay startup.
            Timers.GetOrAdd(dataStream, _ => new Timer(_ => Apply(options, dataStream), null, TimeSpan.Zero, ReapplyInterval));
        }

        internal static string PolicyBody(int deleteAfterDays) =>
            "{\"policy\":{" +
            "\"_meta\":{\"description\":\"Managed by SimplyWorks.Logger.ElasticSearch. Change ElasticsearchDeleteIndexAfterDays instead of editing this policy.\"}," +
            "\"phases\":{" +
            "\"hot\":{\"min_age\":\"0ms\",\"actions\":{\"rollover\":{\"max_age\":\"30d\",\"max_primary_shard_size\":\"50gb\"}}}," +
            $"\"delete\":{{\"min_age\":\"{deleteAfterDays}d\",\"actions\":{{\"delete\":{{}}}}}}" +
            "}}}";

        internal static string SettingsBody(string policyName) =>
            $"{{\"index\":{{\"lifecycle\":{{\"name\":\"{policyName}\"}}}}}}";

        private static void Apply(LoggerOptions options, string dataStream)
        {
            try
            {
                var settings = new ConnectionSettings(new Uri(options.ElasticsearchUrl));
                settings.BasicAuthentication(options.ElasticsearchUser, options.ElasticsearchPassword);
                var client = new ElasticClient(settings).LowLevel;
                var policyName = PolicyName(dataStream);

                var policy = client.DoRequest<StringResponse>(HttpMethod.PUT, $"_ilm/policy/{policyName}",
                    PostData.String(PolicyBody(options.ElasticsearchDeleteIndexAfterDays)));
                if (!policy.Success)
                {
                    Console.WriteLine($"SwLogger: could not save retention policy {policyName}: {policy.HttpStatusCode} {policy.Body}");
                    return;
                }

                // Applies to every backing index of the data stream, including the current one.
                var apply = client.DoRequest<StringResponse>(HttpMethod.PUT, $"{dataStream}/_settings",
                    PostData.String(SettingsBody(policyName)));
                if (apply.HttpStatusCode == 404)
                    Console.WriteLine($"SwLogger: data stream {dataStream} doesn't exist yet; retention will apply on the next run.");
                else if (!apply.Success)
                    Console.WriteLine($"SwLogger: could not apply {policyName} to {dataStream}: {apply.HttpStatusCode} {apply.Body}");
                else
                    Console.WriteLine($"SwLogger: {dataStream} keeps logs for {options.ElasticsearchDeleteIndexAfterDays} days ({policyName}).");
            }
            catch (Exception ex)
            {
                // Retention must never take the application down.
                Console.WriteLine($"SwLogger: applying log retention to {dataStream} failed: {ex.Message}");
            }
        }
    }
}
