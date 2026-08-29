# SW-Logger

[![Build and Publish NuGet Package](https://github.com/simplify9/SW-Logger/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/simplify9/SW-Logger/actions/workflows/nuget-publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

| **Package** | **Version** | **Downloads** |
|:-----------:|:-----------:|:-------------:|
| [SimplyWorks.Logger.ElasticSearch](https://www.nuget.org/packages/SimplyWorks.Logger.ElasticSearch/) | ![Nuget](https://img.shields.io/nuget/v/SimplyWorks.Logger.ElasticSearch) | ![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Logger.ElasticSearch) |
| [SimplyWorks.Logger.Console](https://www.nuget.org/packages/SimplyWorks.Logger.Console/) | ![Nuget](https://img.shields.io/nuget/v/SimplyWorks.Logger.Console) | ![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Logger.Console) |
| [SimplyWorks.Logger.OpenTelemetry](https://www.nuget.org/packages/SimplyWorks.Logger.OpenTelemetry/) | ![Nuget](https://img.shields.io/nuget/v/SimplyWorks.Logger.OpenTelemetry) | ![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Logger.OpenTelemetry) |

## Overview

SW-Logger is a .NET logging library for .NET 8 applications. It provides three logging packages to suit different observability needs:

- **Console Logger** – Enhanced console logging with structured output and request context enrichment (Serilog-based).
- **ElasticSearch Logger** – Structured logging to ElasticSearch with data streams, index lifecycle management, and correlation tracking (Serilog-based).
- **OpenTelemetry Logger** – Vendor-neutral observability (logs, traces, metrics) via OTLP, with optional dual-export to Azure Monitor / Application Insights for seamless migration.

## Features

- 🎯 **Structured Logging** – Serilog (Console/ES) and OpenTelemetry for fully structured, queryable output
- 🔍 **Request Context Enrichment** – Correlation IDs and user identity propagated automatically
- 🗂️ **ElasticSearch Integration** – Data streams and index lifecycle management
- 📡 **OTLP Export** – Send to any OpenTelemetry-compatible backend (Grafana, Jaeger, Honeycomb, etc.)
- 🔵 **Azure Monitor / App Insights** – Dual-export bridge for migrating away from the App Insights SDK
- 📊 **Traces, Metrics & Logs** – Full three-pillar observability via the OTel package
- ⚙️ **Configuration-Driven** – All options available in `appsettings.json`
- 🏷️ **Environment-Aware** – Per-environment exporter activation

---

## Installation

```bash
# Console logger (Serilog-based)
dotnet add package SimplyWorks.Logger.Console

# ElasticSearch logger (Serilog-based)
dotnet add package SimplyWorks.Logger.ElasticSearch

# OpenTelemetry logger (traces + metrics + logs)
dotnet add package SimplyWorks.Logger.OpenTelemetry
```

---

## Console Logger

### Setup

```csharp
// Program.cs / Startup.cs
services.AddSWConsoleLogger(options =>
{
    options.ApplicationName = "MyApp";
    options.LoggingLevel = 1; // Information
});
```

### Options (`appsettings.json`)

```json
{
  "SwLogger": {
    "ApplicationName": "MyApp",
    "LoggingLevel": 1,
    "Environments": "Development,Staging,Production"
  }
}
```

| Option | Default | Description |
|--------|---------|-------------|
| `ApplicationName` | `"unknownapp"` | Reported in every log entry |
| `ApplicationVersion` | Assembly version | Auto-detected |
| `LoggingLevel` | `1` | Serilog level integer |
| `Environments` | all three | Environments where logging is active |

---

## ElasticSearch Logger

### Setup

```csharp
// Program.cs host builder
Host.CreateDefaultBuilder(args)
    .UseSwElasticSearchLogger(options =>
    {
        options.ApplicationName = "my-service";
        options.ElasticsearchUrl = "https://localhost:9200";
    })
    .ConfigureWebHostDefaults(web => web.UseStartup<Startup>());

// Startup.Configure
app.UseRequestContextLogEnricher();
```

### Options (`appsettings.json`)

```json
{
  "SwLogger": {
    "ApplicationName": "my-service",
    "LoggingLevel": 3,
    "ElasticsearchUrl": "https://localhost:9200",
    "ElasticsearchUser": "elastic",
    "ElasticsearchPassword": "password",
    "ElasticsearchEnvironments": "Staging,Production",
    "ElasticsearchCertificatePath": "/certs/ca.crt",
    "ElasticsearchDeleteIndexAfterDays": 90
  }
}
```

| Option | Default | Description |
|--------|---------|-------------|
| `ApplicationName` | `"unknownapp"` | Used as the data-stream name |
| `LoggingLevel` | `3` | Serilog level |
| `ElasticsearchUrl` | — | Cluster URL |
| `ElasticsearchUser` / `Password` | — | Basic auth credentials |
| `ElasticsearchEnvironments` | all three | Environments where ES export is active |
| `ElasticsearchCertificatePath` | — | Path to CA cert (optional) |
| `ElasticsearchDeleteIndexAfterDays` | `90` | ILM delete phase |

---

## OpenTelemetry Logger

Provides **logs, distributed traces, and metrics** via the [OpenTelemetry](https://opentelemetry.io/) SDK.
Supports any OTLP-compatible backend and optional dual-export to **Azure Monitor / Application Insights**.

### Setup

```csharp
// Program.cs / Startup.cs
services.AddSWOpenTelemetryLogger();

// Optional: propagate request context (correlation ID, user) as trace tags
app.UseRequestContextLogEnricher();
```

### Minimal `appsettings.json`

```json
{
  "SwLogger": {
    "ApplicationName": "my-service",
    "OtlpEndpoint": "http://otel-collector:4317"
  }
}
```

### Full Reference (`appsettings.json`)

```json
{
  "SwLogger": {
    "ApplicationName": "my-service",
    "ApplicationVersion": "2.1.0",
    "ServiceInstanceId": "pod-abc123",

    "EnableLogging": true,
    "EnableTracing": true,
    "EnableMetrics": true,

    "SamplingRatio": 0.25,

    "OtlpEndpoint": "http://otel-collector:4317",
    "OtlpProtocol": "Grpc",
    "OtlpExportEnvironments": "Staging,Production",
    "OtlpHeaders": {
      "Authorization": "Basic <base64>",
      "X-Team": "platform"
    },

    "AzureMonitorConnectionString": "InstrumentationKey=...",

    "AdditionalResourceAttributes": {
      "team": "platform",
      "region": "westeurope",
      "cluster": "prod-aks"
    }
  }
}
```

### Options Reference

| Option | Default | Description |
|--------|---------|-------------|
| `ApplicationName` | `"unknownapp"` | `service.name` OTel resource attribute |
| `ApplicationVersion` | Assembly version | `service.version` resource attribute |
| `ServiceInstanceId` | Hostname | `service.instance.id` — identify a specific pod/container |
| `AdditionalResourceAttributes` | `{}` | Extra key/value pairs added to the OTel Resource for every signal |
| `EnableLogging` | `true` | Export logs via OTel |
| `EnableTracing` | `true` | Export distributed traces |
| `EnableMetrics` | `true` | Export metrics (ASP.NET Core, HTTP client, .NET runtime) |
| `SamplingRatio` | `1.0` | Fraction of traces to sample (0.0–1.0). Reduce in high-traffic production to cut costs |
| `OtlpEndpoint` | — | OTLP collector URL (`http://host:4317` for gRPC, `http://host:4318` for HTTP) |
| `OtlpProtocol` | `Grpc` | `Grpc` or `HttpProtobuf` |
| `OtlpExportEnvironments` | all three | Environments where OTLP export is active; falls back to console exporter otherwise |
| `OtlpHeaders` | `{}` | Auth/custom headers sent with every OTLP request (SaaS vendor tokens, etc.) |
| `AzureMonitorConnectionString` | — | When set, **also** exports to Application Insights via Azure Monitor exporter |

### Migrating from Application Insights SDK

OpenTelemetry is the recommended long-term replacement for the Application Insights SDK.
This package supports running **both exporters simultaneously**, giving you a zero-downtime migration path:

**Phase 1 – Dual export** (validate parity)
```json
{
  "SwLogger": {
    "OtlpEndpoint": "http://otel-collector:4317",
    "AzureMonitorConnectionString": "InstrumentationKey=..."
  }
}
```
Both the new OTLP backend and Application Insights receive the same telemetry.

**Phase 2 – Remove App Insights** (once validated)
```json
{
  "SwLogger": {
    "OtlpEndpoint": "http://otel-collector:4317"
  }
}
```
Remove the `AzureMonitorConnectionString` key (or set it to empty). Remove the `Microsoft.ApplicationInsights.*` packages from the consuming project.

---

## Shared Usage Examples

### Injecting and using the logger

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger) => _logger = logger;

    public async Task ProcessOrder(string orderId)
    {
        _logger.LogInformation("Processing order {OrderId}", orderId);
        // ...
    }
}
```

### Adding custom Activity / span tags

```csharp
using System.Diagnostics;

var activity = Activity.Current;
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.tier", "premium");
```

---

## Project Structure

```
SW-Logger/
├── SW.Logger.Console/            # Serilog-based console package
│   ├── IServiceCollectionExtensions.cs
│   └── LoggerOptions.cs
├── SW.Logger.ElasticSearch/      # Serilog-based ES package
│   ├── IAppBuilderExtensions.cs
│   ├── IHostBuilderExtensions.cs
│   └── LoggerOptions.cs
├── SW.Logger.OpenTelemetry/      # OTel package (logs + traces + metrics)
│   ├── IAppBuilderExtensions.cs
│   ├── IServiceCollectionExtensions.cs
│   └── LoggerOptions.cs
└── SW.Logger.SampleWeb/          # Sample web application
```

---

## Dependencies

### Console Logger
- `Serilog.AspNetCore` 8.0.3
- `SimplyWorks.PrimitiveTypes` 8.1.2

### ElasticSearch Logger
- `Elastic.Serilog.Sinks` 8.18.2 · `NEST` 7.17.5 · `Serilog.AspNetCore` 8.0.3
- `SimplyWorks.PrimitiveTypes` 8.1.2

### OpenTelemetry Logger
- `OpenTelemetry.Extensions.Hosting` 1.12.0
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.15.3
- `OpenTelemetry.Exporter.Console` 1.9.0
- `OpenTelemetry.Instrumentation.AspNetCore` 1.9.0
- `OpenTelemetry.Instrumentation.Http` 1.9.0
- `OpenTelemetry.Instrumentation.Runtime` 1.9.0
- `Azure.Monitor.OpenTelemetry.Exporter` 1.4.0
- `SimplyWorks.PrimitiveTypes` 8.1.2

---

## Requirements

- .NET 8.0 or higher
- ElasticSearch 7.x/8.x (for the ES package)
- An OTLP-compatible collector (Grafana Agent, OpenTelemetry Collector, Jaeger, etc.) and/or Azure Monitor (for the OTel package)

## Contributing

Contributions are welcome! Please submit a Pull Request.

## License

MIT — see the [LICENSE](LICENSE) file for details.

## Support

Report bugs or request features via [GitHub Issues](https://github.com/simplify9/SW-Logger/issues).
