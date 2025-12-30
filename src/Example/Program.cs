// Copyright 2024 QuickSearch Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using QuickSearch.Serilog;
using Serilog;
using Serilog.Events;

// Configuration
const string ServerUrl = "http://localhost:3000";
const string ApiKey = "2cbe41dfe1ac71ba836be43a5fcf7eaa2b5071a0ac194847f284e24e9a71fd01";
const string Application = "QuickSearch.Example";

Console.WriteLine("===========================================");
Console.WriteLine("   QuickSearch.Serilog Example Application");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine($"Server URL:  {ServerUrl}");
Console.WriteLine($"Application: {Application}");
Console.WriteLine();

// Configure Serilog with both QuickSearch and Console sinks
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties:j}{NewLine}")
    .WriteTo.QuickSearch(
        serverUrl: ServerUrl,
        apiKey: ApiKey,
        application: Application,
        restrictedToMinimumLevel: LogEventLevel.Debug,
        batchPostingLimit: 100,
        period: TimeSpan.FromSeconds(1))
    .CreateLogger();

Console.WriteLine("Logger configured. Starting log event generation...");
Console.WriteLine("Press Ctrl+C to exit.");
Console.WriteLine();

// Sample data generators
var random = new Random();
var userNames = new[] { "alice", "bob", "charlie", "diana", "eve", "frank" };
var currencies = new[] { "USD", "EUR", "GBP", "JPY", "THB" };
var transactionStatuses = new[] { "completed", "pending", "failed", "refunded" };
var ipAddresses = new[] { "192.168.1.100", "10.0.0.50", "172.16.0.25", "203.0.113.42" };
var errorMessages = new[]
{
    "Connection timeout",
    "Invalid authentication token",
    "Resource not found",
    "Rate limit exceeded",
    "Internal server error"
};

var eventCount = 0;

// Handle Ctrl+C gracefully
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutting down...");
};

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        eventCount++;
        var eventType = random.Next(1, 5);

        switch (eventType)
        {
            case 1:
                // User login event
                var userId = Guid.NewGuid().ToString("N")[..8];
                var userName = userNames[random.Next(userNames.Length)];
                var ipAddress = ipAddresses[random.Next(ipAddresses.Length)];
                var loginSuccess = random.NextDouble() > 0.1;

                if (loginSuccess)
                {
                    Log.Information("User login successful for {UserName} from {IpAddress}",
                        userName, ipAddress);
                    Log.ForContext("Event", new
                    {
                        Type = "UserLogin",
                        UserId = userId,
                        UserName = userName,
                        IpAddress = ipAddress,
                        Success = true,
                        Timestamp = DateTimeOffset.UtcNow
                    })
                        .Information("User authentication completed");
                }
                else
                {
                    Log.Warning("Failed login attempt for {UserName} from {IpAddress}",
                        userName, ipAddress);
                }
                break;

            case 2:
                // Transaction event
                var transactionId = Guid.NewGuid().ToString("N")[..12];
                var amount = Math.Round(random.NextDouble() * 1000, 2);
                var currency = currencies[random.Next(currencies.Length)];
                var status = transactionStatuses[random.Next(transactionStatuses.Length)];

                Log.ForContext("Transaction", new
                {
                    TransactionId = transactionId,
                    Amount = amount,
                    Currency = currency,
                    Status = status,
                    ProcessedAt = DateTimeOffset.UtcNow
                })
                    .Information("Transaction {TransactionId} processed: {Amount} {Currency} - {Status}",
                        transactionId, amount, currency, status);
                break;

            case 3:
                // Performance metrics
                var cpuUsage = Math.Round(random.NextDouble() * 100, 1);
                var memoryUsage = Math.Round(random.NextDouble() * 100, 1);
                var responseTime = random.Next(10, 500);
                var activeConnections = random.Next(1, 100);

                Log.ForContext("Metrics", new
                {
                    Cpu = cpuUsage,
                    Memory = memoryUsage,
                    ResponseTimeMs = responseTime,
                    ActiveConnections = activeConnections,
                    CollectedAt = DateTimeOffset.UtcNow
                })
                    .Debug("System metrics collected: CPU={CpuUsage}%, Memory={MemoryUsage}%, ResponseTime={ResponseTime}ms",
                        cpuUsage, memoryUsage, responseTime);
                break;

            case 4:
                // Error event
                var errorMessage = errorMessages[random.Next(errorMessages.Length)];
                var errorCode = random.Next(1000, 9999);
                var requestId = Guid.NewGuid().ToString("N")[..16];

                try
                {
                    // Simulate an exception
                    throw new InvalidOperationException(errorMessage);
                }
                catch (Exception ex)
                {
                    Log.ForContext("ErrorDetails", new
                    {
                        ErrorCode = errorCode,
                        RequestId = requestId,
                        Component = "ExampleService",
                        OccurredAt = DateTimeOffset.UtcNow
                    })
                        .Error(ex, "Error occurred while processing request {RequestId}: {ErrorMessage}",
                            requestId, errorMessage);
                }
                break;
        }

        // Wait 2-3 seconds between events
        var delay = random.Next(2000, 3001);
        await Task.Delay(delay, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Expected when Ctrl+C is pressed
}
finally
{
    Console.WriteLine($"\nTotal events generated: {eventCount}");
    Console.WriteLine("Flushing logs...");
    await Log.CloseAndFlushAsync();
    Console.WriteLine("Done.");
}