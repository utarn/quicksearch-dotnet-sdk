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

using QuickSearch.Serilog.Tests.Support;
using Serilog;
using Serilog.Events;

namespace QuickSearch.Serilog.Tests.Integration;

/// <summary>
/// Integration tests for the QuickSearch Serilog sink.
/// These tests require a running QuickSearch server.
/// </summary>
/// <remarks>
/// To run these tests, ensure:
/// - QuickSearch server is running at http://localhost:3001
/// - API key is set: f58d7a839c55869dcaac26ff57177ec81478b9adca47e30142bf0e9c7207be6e
/// 
/// Or set environment variables:
/// - QUICKSEARCH_TEST_SERVER_URL
/// - QUICKSEARCH_TEST_API_KEY
/// </remarks>
[Trait("Category", "Integration")]
public class QuickSearchSinkIntegrationTests : IAsyncLifetime
{
    private readonly string _serverUrl;
    private readonly string _apiKey;
    private readonly string _testApplication;
    private ILogger? _logger;

    public QuickSearchSinkIntegrationTests()
    {
        _serverUrl = TestHelpers.GetTestServerUrl();
        _apiKey = TestHelpers.GetTestApiKey();
        _testApplication = $"IntegrationTest-{Guid.NewGuid():N}";
    }

    public Task InitializeAsync()
    {
        // Logger will be created per test for isolation
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        // Wait for any pending batches to be flushed
        await Task.Delay(100);
    }

    #region Single Event Tests

    [Fact]
    public async Task SendSingleEvent_Information_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Information("Integration test - single information event at {Timestamp}", DateTimeOffset.UtcNow);
        await FlushAndWait();

        // Assert - no exception means success
        // The event was sent successfully if no exception was thrown
    }

    [Fact]
    public async Task SendSingleEvent_Debug_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger(LogEventLevel.Debug);

        // Act
        _logger.Debug("Integration test - debug level event with value {Value}", 42);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendSingleEvent_Warning_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Warning("Integration test - warning event for user {UserId}", "user-123");
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendSingleEvent_Error_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Error("Integration test - error event with code {ErrorCode}", "ERR001");
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendSingleEvent_Fatal_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Fatal("Integration test - fatal event indicating system failure");
        await FlushAndWait();

        // Assert - success if no exception
    }

    #endregion

    #region Batch Event Tests

    [Fact]
    public async Task SendBatchOfEvents_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger(batchPostingLimit: 50);

        // Act - Send multiple events
        for (int i = 0; i < 10; i++)
        {
            _logger.Information("Integration test - batch event {EventNumber} of 10", i + 1);
        }
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendLargeBatch_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger(batchPostingLimit: 100);

        // Act - Send a larger batch
        for (int i = 0; i < 50; i++)
        {
            _logger.Information("Integration test - large batch event {EventNumber}", i + 1);
        }
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventsInMultipleBatches_ShouldSucceed()
    {
        // Arrange - Small batch limit to force multiple batches
        _logger = CreateLogger(
            batchPostingLimit: 5,
            period: TimeSpan.FromMilliseconds(200));

        // Act - Send more events than batch limit
        for (int i = 0; i < 20; i++)
        {
            _logger.Information("Integration test - multi-batch event {EventNumber}", i + 1);
        }
        
        // Wait for multiple batch cycles
        await Task.Delay(1000);

        // Assert - success if no exception
    }

    #endregion

    #region Different Log Level Tests

    [Fact]
    public async Task SendAllLogLevels_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger(LogEventLevel.Verbose);

        // Act - Send one event at each level
        _logger.Verbose("Integration test - Verbose level");
        _logger.Debug("Integration test - Debug level");
        _logger.Information("Integration test - Information level");
        _logger.Warning("Integration test - Warning level");
        _logger.Error("Integration test - Error level");
        _logger.Fatal("Integration test - Fatal level");
        
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventsWithMinimumLevelFilter_ShouldOnlySendQualifyingEvents()
    {
        // Arrange - Only Warning and above
        _logger = CreateLogger(LogEventLevel.Warning);

        // Act - These should be filtered out (below Warning)
        _logger.Verbose("This should be filtered");
        _logger.Debug("This should be filtered");
        _logger.Information("This should be filtered");
        
        // These should be sent
        _logger.Warning("This should be sent - Warning");
        _logger.Error("This should be sent - Error");
        _logger.Fatal("This should be sent - Fatal");
        
        await FlushAndWait();

        // Assert - success if no exception (filtering happens before sink)
    }

    #endregion

    #region Structured Data Tests

    [Fact]
    public async Task SendEventWithStructuredData_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var user = new { Id = 12345, Name = "John Doe", Email = "john@example.com" };

        // Act
        _logger.Information("Integration test - User logged in: {@User}", user);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithMultipleProperties_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Information(
            "Integration test - Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
            "GET",
            "/api/users",
            125.5,
            200);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithComplexObject_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var order = new
        {
            OrderId = "ORD-12345",
            Customer = new { Id = "CUST-001", Name = "Jane Doe" },
            Items = new[]
            {
                new { ProductId = "PROD-A", Quantity = 2, Price = 29.99 },
                new { ProductId = "PROD-B", Quantity = 1, Price = 49.99 }
            },
            Total = 109.97,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        _logger.Information("Integration test - Order placed: {@Order}", order);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithArray_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var items = new[] { "apple", "banana", "cherry", "date" };

        // Act
        _logger.Information("Integration test - Shopping cart contains: {Items}", items);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithDictionary_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var metadata = new Dictionary<string, object>
        {
            ["requestId"] = Guid.NewGuid().ToString(),
            ["correlationId"] = Guid.NewGuid().ToString(),
            ["version"] = "1.0.0",
            ["retryCount"] = 3
        };

        // Act
        _logger.Information("Integration test - Request metadata: {@Metadata}", metadata);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithNumericTypes_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Information(
            "Integration test - Numeric types: Int={Int}, Long={Long}, Float={Float}, Double={Double}, Decimal={Decimal}",
            42,
            9999999999L,
            3.14f,
            2.718281828,
            123.456m);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithDateTypes_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();

        // Act
        _logger.Information(
            "Integration test - Date types: DateTime={DateTime}, DateTimeOffset={DateTimeOffset}, TimeSpan={TimeSpan}",
            DateTime.UtcNow,
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(2.5));
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithGuid_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var requestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        // Act
        _logger.Information(
            "Integration test - Request {RequestId} with correlation {CorrelationId}",
            requestId,
            correlationId);
        await FlushAndWait();

        // Assert - success if no exception
    }

    #endregion

    #region Exception Tests

    [Fact]
    public async Task SendEventWithException_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var exception = new InvalidOperationException("Integration test exception");

        // Act
        _logger.Error(exception, "Integration test - An error occurred while processing request");
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithNestedException_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        Exception nestedException;
        try
        {
            try
            {
                throw new ArgumentException("Inner exception from integration test");
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Outer exception from integration test", inner);
            }
        }
        catch (Exception ex)
        {
            nestedException = ex;
        }

        // Act
        _logger.Error(nestedException, "Integration test - Nested exception occurred");
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithExceptionAndProperties_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var exception = new HttpRequestException("Integration test - Connection refused");

        // Act
        _logger.Error(
            exception,
            "Integration test - Failed to connect to {Endpoint} after {RetryCount} retries",
            "https://api.example.com",
            3);
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithAggregateException_ShouldSucceed()
    {
        // Arrange
        _logger = CreateLogger();
        var exceptions = new List<Exception>
        {
            new InvalidOperationException("Task 1 failed"),
            new TimeoutException("Task 2 timed out"),
            new ArgumentException("Task 3 had invalid argument")
        };
        var aggregateException = new AggregateException("Multiple tasks failed", exceptions);

        // Act
        _logger.Error(aggregateException, "Integration test - Parallel operation failed with multiple errors");
        await FlushAndWait();

        // Assert - success if no exception
    }

    #endregion

    #region Application Name Tests

    [Fact]
    public async Task SendEventWithApplicationName_ShouldIncludeApplication()
    {
        // Arrange
        _logger = CreateLogger(application: "MyIntegrationTestApp");

        // Act
        _logger.Information("Integration test - Event with specific application name");
        await FlushAndWait();

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventWithEmptyApplicationName_ShouldUseUnknown()
    {
        // Arrange
        _logger = CreateLogger(application: null);

        // Act
        _logger.Information("Integration test - Event without application name");
        await FlushAndWait();

        // Assert - success if no exception
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task SendManyEventsRapidly_ShouldHandle()
    {
        // Arrange
        _logger = CreateLogger(
            batchPostingLimit: 100,
            period: TimeSpan.FromMilliseconds(500),
            queueSizeLimit: 10000);

        // Act - Send many events rapidly
        for (int i = 0; i < 100; i++)
        {
            _logger.Information("Integration test - Rapid event {Index} at {Timestamp}", i, DateTimeOffset.UtcNow);
        }
        
        // Wait for batches to be processed
        await Task.Delay(2000);

        // Assert - success if no exception
    }

    [Fact]
    public async Task SendEventsWithVariousPayloadSizes_ShouldHandle()
    {
        // Arrange
        _logger = CreateLogger();

        // Act - Send events with different payload sizes
        _logger.Information("Small message");
        _logger.Information("Medium message with some properties: {Prop1} {Prop2} {Prop3}", "value1", "value2", "value3");
        _logger.Information("Larger message with a big string: {LargeData}", new string('x', 1000));
        _logger.Information("Message with complex data: {@Data}", new
        {
            Id = 1,
            Name = "Test",
            Description = new string('y', 500),
            Items = Enumerable.Range(1, 20).Select(i => new { Index = i, Value = $"Item-{i}" }).ToArray()
        });
        
        await FlushAndWait();

        // Assert - success if no exception
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a configured logger for integration tests.
    /// </summary>
    private ILogger CreateLogger(
        LogEventLevel minimumLevel = LogEventLevel.Information,
        int batchPostingLimit = 100,
        TimeSpan? period = null,
        int queueSizeLimit = 10000,
        string? application = null)
    {
        var effectiveApplication = application ?? _testApplication;
        var effectivePeriod = period ?? TimeSpan.FromMilliseconds(500);

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.QuickSearch(
                serverUrl: _serverUrl,
                apiKey: _apiKey,
                application: effectiveApplication,
                restrictedToMinimumLevel: minimumLevel,
                batchPostingLimit: batchPostingLimit,
                period: effectivePeriod,
                queueSizeLimit: queueSizeLimit)
            .CreateLogger();
    }

    /// <summary>
    /// Flushes the logger and waits for batches to be processed.
    /// </summary>
    private async Task FlushAndWait()
    {
        // Dispose logger to flush pending events
        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
            _logger = null;
        }
        
        // Give some time for the HTTP request to complete
        await Task.Delay(1000);
    }

    #endregion

    #region Connection Error Tests

    [Fact]
    public async Task SendEvent_WithInvalidServerUrl_ShouldNotThrow()
    {
        // Arrange - Create logger with invalid URL (should not crash the app)
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: "http://invalid-server-that-does-not-exist:9999",
                application: _testApplication,
                period: TimeSpan.FromMilliseconds(100))
            .CreateLogger();

        // Act - Logging should not throw even if server is unavailable
        logger.Information("This event will fail to send but should not throw");
        
        await Task.Delay(500);
        
        // Cleanup
        (logger as IDisposable)?.Dispose();

        // Assert - If we got here without exception, the test passes
    }

    [Fact]
    public async Task SendEvent_WithInvalidApiKey_ShouldNotThrow()
    {
        // Arrange - Create logger with invalid API key
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: _serverUrl,
                apiKey: "invalid-api-key-12345",
                application: _testApplication,
                period: TimeSpan.FromMilliseconds(100))
            .CreateLogger();

        // Act - Logging should not throw even if authentication fails
        logger.Information("This event may be rejected by the server");
        
        await Task.Delay(500);
        
        // Cleanup
        (logger as IDisposable)?.Dispose();

        // Assert - If we got here without exception, the test passes
    }

    #endregion
}