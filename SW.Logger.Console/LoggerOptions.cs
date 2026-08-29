namespace SW.Logger.Console;

/// <summary>
/// Configuration options for the SW Console Logger.
/// Bind from the <c>SwLogger</c> section of <c>appsettings.json</c> or supply via the
/// <c>configure</c> callback on <see cref="IServiceCollectionExtensions.AddSWConsoleLogger"/>.
/// </summary>
public class LoggerOptions
{
    /// <summary>The configuration section key used to bind these options: <c>"SwLogger"</c>.</summary>
    public const string ConfigurationSection = "SwLogger";

    /// <summary>Initialises a new instance with sensible defaults.</summary>
    public LoggerOptions()
    {
        Environments = "Development,Staging,Production";
        LoggingLevel = 1;
        ApplicationName = "unknownapp";
    }

    /// <summary>
    /// Serilog minimum log level as an integer.
    /// Maps to <see cref="Serilog.Events.LogEventLevel"/>:
    /// 0 = Verbose, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Fatal.
    /// Default: <c>1</c> (Debug).
    /// </summary>
    public int LoggingLevel { get; set; }

    /// <summary>
    /// Application name enriched on every log entry (<c>Application</c> property).
    /// Default: <c>"unknownapp"</c>.
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// Application version enriched on every log entry (<c>ApplicationVersion</c> property).
    /// Auto-populated from the calling assembly when not set explicitly.
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// Comma-separated list of environment names in which logging is active.
    /// Default: <c>"Development,Staging,Production"</c>.
    /// </summary>
    public string Environments { get; set; }

}