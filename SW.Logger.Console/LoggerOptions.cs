namespace SW.Logger.Console;

public class LoggerOptions
{
    public const string ConfigurationSection = "SwLogger";

    public LoggerOptions()
    {
        Environments = "Development,Staging,Production";
        LoggingLevel = 2;
        ApplicationName = "unknownapp";
        LogRequests = true;
        QuietRequestPaths = "/health,/healthz,/metrics";
        SlowQueryMilliseconds = 500;
    }

    /// <summary>
    /// Minimum level, as the numeric value of Serilog's LogEventLevel:
    /// 0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Fatal. Default 2.
    /// </summary>
    public int LoggingLevel { get; set; }
    public string ApplicationName { get; set; }
    public string ApplicationVersion { get; set; }
    public string Environments { get; set; }

    /// <summary>
    /// Write one completed-request line per HTTP request (with a <c>Route</c> template property),
    /// even when <see cref="LoggingLevel"/> is above Information. Default true.
    /// </summary>
    public bool LogRequests { get; set; }

    /// <summary>
    /// Comma-separated path prefixes (health probes, metrics scrapes) whose successful requests
    /// are logged at Verbose, i.e. dropped at the default level. Failures are still logged.
    /// </summary>
    public string QuietRequestPaths { get; set; }

    /// <summary>
    /// EF Core commands that take at least this long are logged (as "Executed DbCommand (Nms)"),
    /// so a slow request can be traced to its query. Faster commands are not logged, which keeps
    /// volume down. 0 or less disables slow-query logging. Default 500.
    /// </summary>
    public int SlowQueryMilliseconds { get; set; }

    /// <summary>
    /// Properties added to every log line, such as which deployment wrote it. Set in code, or under
    /// <c>SwLogger:Properties</c> in configuration (configuration wins for the same name). A name the logger sets
    /// itself (Environment, Application, ApplicationVersion) keeps its value, and an empty value is left out.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Comma-separated W3C <c>baggage</c> keys (https://www.w3.org/TR/baggage/) copied onto the request line,
    /// each as a property of the same name, e.g. <c>ui.page,ui.session</c> for the page and browser tab a call
    /// came from. Only these keys are read: baggage is set by the caller, so an open list would let any client
    /// add properties. Values are cut to a fixed length. Empty (the default) reads none.
    /// </summary>
    public string BaggageKeys { get; set; } = string.Empty;
}
