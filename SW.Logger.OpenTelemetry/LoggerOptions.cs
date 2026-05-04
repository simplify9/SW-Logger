namespace SW.Logger.OpenTelemetry;

/// <summary>
/// Configuration options for the SW OpenTelemetry Logger.
/// Bind from the <c>SwLogger</c> section of <c>appsettings.json</c> or supply via the
/// <c>configure</c> callback on <see cref="IServiceCollectionExtensions.AddSWOpenTelemetryLogger"/>.
/// </summary>
public class LoggerOptions
{
    /// <summary>The configuration section key used to bind these options: <c>"SwLogger"</c>.</summary>
    public const string ConfigurationSection = "SwLogger";

    /// <summary>Initialises a new instance with sensible defaults.</summary>
    public LoggerOptions()
    {
        ApplicationName = "unknownapp";
        OtlpExportEnvironments = "Development,Staging,Production";
        EnableTracing = true;
        EnableMetrics = true;
        EnableLogging = true;
        SamplingRatio = 1.0;
        OtlpProtocol = OtlpProtocolType.Grpc;
        OtlpHeaders = new Dictionary<string, string>();
        AdditionalResourceAttributes = new Dictionary<string, string>();
    }

    // ── Service identity ──────────────────────────────────────────────────────

    /// <summary>
    /// Service name reported to every OTel signal (service.name resource attribute).
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Service version (service.version resource attribute).
    /// Auto-populated from the calling assembly when not set explicitly.
    /// </summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Unique identifier for this running instance (service.instance.id resource attribute).
    /// Defaults to the machine hostname when not set.
    /// Useful for correlating signals from a specific pod/container.
    /// </summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>
    /// Additional key/value pairs merged into the OTel Resource for every signal.
    /// Useful for custom environment metadata (team, region, cluster, etc.).
    /// </summary>
    public Dictionary<string, string> AdditionalResourceAttributes { get; set; }

    // ── Signal toggles ────────────────────────────────────────────────────────

    /// <summary>Enable distributed tracing via OpenTelemetry.</summary>
    public bool EnableTracing { get; set; }

    /// <summary>Enable metrics via OpenTelemetry.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>Enable log export via OpenTelemetry.</summary>
    public bool EnableLogging { get; set; }

    // ── Sampling ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fraction of traces to sample (0.0 – 1.0, default 1.0 = 100%).
    /// Use a lower value in high-traffic production environments to reduce cost.
    /// </summary>
    public double SamplingRatio { get; set; }

    // ── OTLP exporter ─────────────────────────────────────────────────────────

    /// <summary>
    /// Comma-separated list of environments in which OTLP export is active.
    /// </summary>
    public string OtlpExportEnvironments { get; set; }

    /// <summary>
    /// OTLP collector endpoint URL.
    /// gRPC default  : http://localhost:4317
    /// HTTP/protobuf : http://localhost:4318
    /// When null, the OTEL_EXPORTER_OTLP_ENDPOINT environment variable is used.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Wire protocol used by the OTLP exporter.
    /// <see cref="OtlpProtocolType.Grpc"/> (default) or <see cref="OtlpProtocolType.HttpProtobuf"/>.
    /// </summary>
    public OtlpProtocolType OtlpProtocol { get; set; }

    /// <summary>
    /// Headers sent with every OTLP request.
    /// Required by many SaaS vendors (Grafana Cloud, Honeycomb, Lightstep, etc.)
    /// for authentication tokens.
    /// Example: { "Authorization": "Basic &lt;base64&gt;" }
    /// </summary>
    public Dictionary<string, string> OtlpHeaders { get; set; }

    // ── Azure Monitor / Application Insights ──────────────────────────────────

    /// <summary>
    /// Azure Monitor (Application Insights) connection string.
    /// When set, all signals are additionally exported to Application Insights
    /// via the Azure Monitor OpenTelemetry exporter.
    /// This lets you run both OTLP and Application Insights simultaneously,
    /// which is the recommended migration path when phasing out the App Insights SDK.
    /// Set to null/empty once the migration is complete.
    /// </summary>
    public string? AzureMonitorConnectionString { get; set; }
}

/// <summary>Wire protocol for the OTLP exporter.</summary>
public enum OtlpProtocolType
{
    /// <summary>gRPC (port 4317 by default)</summary>
    Grpc,
    /// <summary>HTTP/protobuf (port 4318 by default)</summary>
    HttpProtobuf
}
