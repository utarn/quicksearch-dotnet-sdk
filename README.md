# QuickSearch.Serilog

A Serilog sink for sending structured log events to a QuickSearch logging server.

[![NuGet](https://img.shields.io/nuget/v/QuickSearch.Serilog.svg)](https://www.nuget.org/packages/QuickSearch.Serilog)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

## Features

- **Batched Event Ingestion**: Efficiently sends log events in batches to reduce HTTP overhead
- **Structured Logging**: Full support for Serilog's structured logging with properties
- **Resilient**: Automatic retry with exponential backoff on failures
- **Non-blocking**: Logging operations don't block your application
- **Configurable**: Flexible configuration options for batch size, timing, and queue limits

## Installation

```bash
dotnet add package QuickSearch.Serilog
```

Or via the Package Manager Console:

```powershell
Install-Package QuickSearch.Serilog
```

## Quick Start

### Basic Usage

```csharp
using Serilog;
using QuickSearch.Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.QuickSearch(
        serverUrl: "http://localhost:3001",
        apiKey: "your-api-key")
    .CreateLogger();

// Log some events
Log.Information("Application started");
Log.Warning("This is a warning with {Property}", "value");

// Important: flush before application exit
Log.CloseAndFlush();
```

### With Application Name

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.QuickSearch(
        serverUrl: "http://localhost:3001",
        apiKey: "your-api-key",
        application: "MyWebApp")
    .CreateLogger();
```

### Advanced Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.QuickSearch(
        serverUrl: "http://localhost:3001",
        apiKey: "your-api-key",
        application: "MyWebApp",
        restrictedToMinimumLevel: LogEventLevel.Information,
        batchPostingLimit: 500,
        period: TimeSpan.FromSeconds(5),
        queueSizeLimit: 50000,
        eventBodyLimitBytes: 262144) // 256KB max per event
    .CreateLogger();
```

### Using Options Object

```csharp
var options = new QuickSearchSinkOptions
{
    ServerUrl = "http://localhost:3001",
    ApiKey = "your-api-key",
    Application = "MyWebApp",
    BatchPostingLimit = 500,
    Period = TimeSpan.FromSeconds(5),
    RestrictedToMinimumLevel = LogEventLevel.Information
};

Log.Logger = new LoggerConfiguration()
    .WriteTo.QuickSearch(options)
    .CreateLogger();
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `serverUrl` | string | (required) | QuickSearch server URL |
| `apiKey` | string? | null | API key for Bearer token authentication |
| `application` | string? | null | Application name to tag events |
| `restrictedToMinimumLevel` | LogEventLevel | Verbose | Minimum log level to send |
| `batchPostingLimit` | int | 1000 | Maximum events per batch |
| `period` | TimeSpan | 2 seconds | Time between batch sends |
| `queueSizeLimit` | int | 100000 | Maximum queued events |
| `eventBodyLimitBytes` | long? | null | Max size per event (bytes) |
| `messageHandler` | HttpMessageHandler? | null | Custom HTTP handler |

## Event Format

Events are sent to the QuickSearch API in the following JSON format:

```json
{
  "type": "Information",
  "application": "MyWebApp",
  "timestamp": "2024-12-29T10:30:00.000Z",
  "message": "User 12345 logged in from 192.168.1.1",
  "data": {
    "messageTemplate": "User {UserId} logged in from {IpAddress}",
    "level": "Information",
    "UserId": "12345",
    "IpAddress": "192.168.1.1",
    "traceId": "abc123",
    "spanId": "def456"
  }
}
```

## Log Level Mapping

| Serilog Level | QuickSearch Type |
|--------------|------------------|
| Verbose | Trace |
| Debug | Debug |
| Information | Information |
| Warning | Warning |
| Error | Error |
| Fatal | Critical |

## Structured Logging Examples

### With Properties

```csharp
Log.Information("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

### With Exceptions

```csharp
try
{
    // ... some code
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to process order {OrderId}", orderId);
}
```

### With Complex Objects

```csharp
var order = new { Id = 123, Items = new[] { "A", "B", "C" } };
Log.Information("Processing order {@Order}", order);
```

## Self-Diagnostics

Enable internal logging to diagnose issues:

```csharp
using QuickSearch.Serilog.Internal;

// Enable self-logging to console
SelfLog.Enable(Console.Error.WriteLine);

// Or to a file
SelfLog.Enable(msg => File.AppendAllText("serilog-selflog.txt", msg + Environment.NewLine));
```

## ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.QuickSearch(
            serverUrl: context.Configuration["QuickSearch:ServerUrl"]!,
            apiKey: context.Configuration["QuickSearch:ApiKey"],
            application: context.Configuration["QuickSearch:Application"]);
});

var app = builder.Build();
app.Run();
```

With `appsettings.json`:

```json
{
  "QuickSearch": {
    "ServerUrl": "http://localhost:3001",
    "ApiKey": "your-api-key",
    "Application": "MyWebApi"
  }
}
```

## Thread Safety

The sink is fully thread-safe and can be used from multiple threads concurrently.

## Performance Considerations

- Events are batched to reduce HTTP overhead
- The queue has a configurable size limit to prevent memory exhaustion
- Failed batches are retried with exponential backoff
- After repeated failures, events are dropped to protect the application

## Example Application

A complete example console application is included in the `src/Example` directory. It demonstrates:

- Configuring Serilog with both QuickSearch and Console sinks
- Generating various types of structured log events
- User login events with userId, userName, ipAddress
- Transaction events with transactionId, amount, currency, status
- Performance metrics with cpu, memory, responseTime
- Error events with exception details

### Running the Example

```bash
cd lib/dotnet
dotnet run --project src/Example
```

The example will run in a loop, sending random log events every 2-3 seconds. Press Ctrl+C to stop.

## Requirements

- .NET 8.0 or later
- Serilog 4.2.0 or later

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Related Projects

- [Serilog](https://serilog.net/) - The structured logging framework
- [QuickSearch](https://github.com/your-org/quicksearch) - The QuickSearch logging server