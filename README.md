# SW-Logger

[![Build and Publish NuGet Package](https://github.com/simplify9/SW-Logger/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/simplify9/SW-Logger/actions/workflows/nuget-publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

| **Package**       | **Version** | **Downloads** |
| :----------------:|:-----------:|:-------------:|
|[SimplyWorks.Logger.ElasticSearch](https://www.nuget.org/packages/SimplyWorks.Logger.ElasticSearch/)| ![Nuget](https://img.shields.io/nuget/v/SimplyWorks.Logger.ElasticSearch) | ![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Logger.ElasticSearch) |
|[SimplyWorks.Logger.Console](https://www.nuget.org/packages/SimplyWorks.Logger.Console/)| ![Nuget](https://img.shields.io/nuget/v/SimplyWorks.Logger.Console) | ![Nuget](https://img.shields.io/nuget/dt/SimplyWorks.Logger.Console) |

## Overview

SW-Logger is a .NET logging library built on top of [Serilog](https://serilog.net) that provides structured logging capabilities for .NET 8.0 applications. It offers two main logging sinks:

- **Console Logger**: Provides enhanced console logging with structured output and request context enrichment
- **ElasticSearch Logger**: Enables logging to ElasticSearch with data streams, lifecycle policies, and correlation tracking

## Features

- 🎯 **Structured Logging**: Built on Serilog for structured, searchable logs
- 🔍 **Request Context Enrichment**: Automatically enriches logs with correlation IDs and user information
- 🗂️ **ElasticSearch Integration**: Direct logging to ElasticSearch with data streams and index lifecycle management
- 📊 **Console Logging**: Enhanced console output with JSON formatting in production
- ⚙️ **Configuration-Driven**: Flexible configuration through `appsettings.json`
- 🔄 **Request Logging**: Built-in HTTP request/response logging
- 🏷️ **Environment Support**: Environment-specific logging configurations

## Installation

### Console Logger

```bash
dotnet add package SimplyWorks.Logger.Console
```

### ElasticSearch Logger

```bash
dotnet add package SimplyWorks.Logger.ElasticSearch
```

## Quick Start

### Console Logger Setup

#### 1. Configure Services

```csharp
using SW.Logger.Console;

public void ConfigureServices(IServiceCollection services)
{
    services.AddSWConsoleLogger(options =>
    {
        options.ApplicationName = "MyApp";
        options.LoggingLevel = 1; // Information level
    });
}
```

#### 2. Configure Application Pipeline

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseSWConsoleLogger();
    // ... other middleware
}
```

### ElasticSearch Logger Setup

#### 1. Configure Host Builder

```csharp
using SW.Logger.ElasticSerach;

public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
        .UseSwElasticSearchLogger(options =>
        {
            options.ApplicationName = "MyApp";
            options.ElasticsearchUrl = "https://localhost:9200";
        });
```

#### 2. Configure Application Pipeline

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRequestContextLogEnricher();
    // ... other middleware
}
```

## Configuration

### appsettings.json Configuration

```json
{
  "SwLogger": {
    "ApplicationName": "MyApplication",
    "LoggingLevel": 1,
    "ElasticsearchUrl": "https://localhost:9200",
    "ElasticsearchUser": "elastic",
    "ElasticsearchPassword": "password",
    "ElasticsearchEnvironments": "Development,Staging,Production",
    "ElasticsearchCertificatePath": "/path/to/certificate.crt",
    "ElasticsearchDeleteIndexAfterDays": 90
  }
}
```

### Configuration Options

#### Console Logger Options

- `ApplicationName`: Name of your application (default: "unknownapp")
- `ApplicationVersion`: Version of your application (auto-detected from assembly)
- `LoggingLevel`: Serilog logging level (1=Information, 2=Debug, etc.)
- `Environments`: Comma-separated list of environments where logging is active

#### ElasticSearch Logger Options

- `ApplicationName`: Name of your application (used for index naming)
- `ApplicationVersion`: Version of your application
- `LoggingLevel`: Serilog logging level
- `ElasticsearchUrl`: ElasticSearch cluster URL
- `ElasticsearchUser`: ElasticSearch username
- `ElasticsearchPassword`: ElasticSearch password
- `ElasticsearchEnvironments`: Environments where ElasticSearch logging is enabled
- `ElasticsearchCertificatePath`: Path to SSL certificate (optional)
- `ElasticsearchDeleteIndexAfterDays`: Days after which indices are deleted (default: 90)

## Usage Examples

### Basic Logging

```csharp
using Microsoft.Extensions.Logging;

public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Get()
    {
        _logger.LogInformation("Processing GET request for Home");
        
        try
        {
            // Your logic here
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GET request");
            throw;
        }
    }
}
```

### Structured Logging with Context

```csharp
_logger.LogInformation("User {UserId} accessed resource {ResourceId}", 
    userId, resourceId);

_logger.LogWarning("Failed login attempt for {Username} from {IpAddress}", 
    username, ipAddress);
```

## Project Structure

```
SW-Logger/
├── SW.Logger.Console/           # Console logging package
│   ├── IAppBuilderExtensions.cs
│   ├── IServiceCollectionExtensions.cs
│   └── LoggerOptions.cs
├── SW.Logger.ElasticSearch/     # ElasticSearch logging package
│   ├── IAppBuilderExtensions.cs
│   ├── IHostBuilderExtensions.cs
│   ├── LoggerOptions.cs
│   └── StringExtensions.cs
└── SW.Logger.SampleWeb/         # Sample web application
    ├── Program.cs
    ├── Startup.cs
    └── appsettings.json
```

## ElasticSearch Features

### Data Streams
The ElasticSearch logger uses data streams with the naming pattern: `logs-{application-name}-{environment}`

### Index Lifecycle Management
Automatically creates and applies lifecycle policies that delete indices after the configured retention period.

### Authentication
Supports both basic authentication and certificate-based authentication for ElasticSearch clusters.

## Dependencies

### Console Logger
- Serilog.AspNetCore (8.0.3)
- SimplyWorks.PrimitiveTypes (8.0.0)

### ElasticSearch Logger
- Elastic.Serilog.Sinks (8.18.2)
- NEST (7.17.5)
- Serilog.AspNetCore (8.0.3)
- SimplyWorks.PrimitiveTypes (8.0.0)

## Requirements

- .NET 8.0 or higher
- For ElasticSearch logging: ElasticSearch cluster (7.x or 8.x)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any bugs or have feature requests, please submit an [issue](https://github.com/simplify9/SW-Logger/issues) on GitHub.

