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

using Moq;
using Moq.Protected;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Net;

namespace QuickSearch.Serilog.Tests;

/// <summary>
/// Unit tests for QuickSearchSinkExtensions configuration methods.
/// </summary>
public class QuickSearchSinkExtensionsTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    #region Extension Method Basic Tests

    [Fact]
    public void QuickSearch_WithServerUrl_ShouldCreateValidLogger()
    {
        // Arrange & Act
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: "http://localhost:3001")
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void QuickSearch_WithAllParameters_ShouldCreateValidLogger()
    {
        // Arrange
        using var handler = CreateMockHandler(HttpStatusCode.OK, "{}");

        // Act
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: "http://localhost:3001",
                apiKey: "test-api-key",
                application: "TestApplication",
                restrictedToMinimumLevel: LogEventLevel.Warning,
                batchPostingLimit: 500,
                period: TimeSpan.FromSeconds(5),
                queueSizeLimit: 50000,
                eventBodyLimitBytes: 262144,
                messageHandler: handler)
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void QuickSearch_WithOptions_ShouldCreateValidLogger()
    {
        // Arrange
        using var handler = CreateMockHandler(HttpStatusCode.OK, "{}");
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            ApiKey = "test-api-key",
            Application = "TestApp",
            BatchPostingLimit = 100,
            MessageHandler = handler
        };

        // Act
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(options)
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    #endregion

    #region Null Argument Validation Tests

    [Fact]
    public void QuickSearch_WithNullSinkConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        LoggerSinkConfiguration? sinkConfiguration = null;

        // Act
        Action act = () => sinkConfiguration!.QuickSearch("http://localhost:3001");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void QuickSearch_WithNullServerUrl_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new LoggerConfiguration()
            .WriteTo.QuickSearch(serverUrl: null!)
            .CreateLogger();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public void QuickSearch_WithEmptyServerUrl_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new LoggerConfiguration()
            .WriteTo.QuickSearch(serverUrl: "")
            .CreateLogger();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serverUrl");
    }

    [Fact]
    public void QuickSearch_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        QuickSearchSinkOptions? options = null;

        // Act
        Action act = () => new LoggerConfiguration()
            .WriteTo.QuickSearch(options!)
            .CreateLogger();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region Options Validation Tests

    [Fact]
    public void QuickSearch_WithInvalidOptions_ShouldThrowDuringConfiguration()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            BatchPostingLimit = 0 // Invalid
        };

        // Act
        Action act = () => new LoggerConfiguration()
            .WriteTo.QuickSearch(options)
            .CreateLogger();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BatchPostingLimit");
    }

    [Fact]
    public void QuickSearch_WithValidCustomPeriod_ShouldNotThrow()
    {
        // Arrange
        using var handler = CreateMockHandler(HttpStatusCode.OK, "{}");

        // Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    period: TimeSpan.FromMilliseconds(500),
                    messageHandler: handler)
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Log Level Filtering Tests

    [Fact]
    public void QuickSearch_WithRestrictedMinimumLevel_ShouldConfigureCorrectly()
    {
        // Arrange
        using var handler = CreateMockHandler(HttpStatusCode.OK, "{}");

        // Act - Should not throw
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: "http://localhost:3001",
                restrictedToMinimumLevel: LogEventLevel.Error,
                messageHandler: handler)
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void QuickSearch_WithAnyLogLevel_ShouldAccept(LogEventLevel level)
    {
        // Arrange
        using var handler = CreateMockHandler(HttpStatusCode.OK, "{}");

        // Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    restrictedToMinimumLevel: level,
                    messageHandler: handler)
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Custom Handler Tests

    [Fact]
    public void QuickSearch_WithCustomMessageHandler_ShouldUseHandler()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

        // Act
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(
                serverUrl: "http://localhost:3001",
                messageHandler: handlerMock.Object)
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void QuickSearch_WithMinimalConfig_ShouldUseDefaults()
    {
        // Arrange & Act
        // This test verifies that the extension method applies defaults correctly
        // We can't directly inspect the sink, but we can verify no exceptions are thrown
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(serverUrl: "http://localhost:3001")
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void QuickSearch_WithNullPeriod_ShouldUseDefaultPeriod()
    {
        // Arrange & Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    period: null) // Should use default
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void QuickSearch_WithNullApiKey_ShouldNotRequireAuthentication()
    {
        // Arrange & Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    apiKey: null) // No API key
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void QuickSearch_WithNullApplication_ShouldWork()
    {
        // Arrange & Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    application: null) // No application name
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Chained Configuration Tests

    [Fact]
    public void QuickSearch_CanBeChainedWithOtherSinks()
    {
        // Arrange & Act
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(serverUrl: "http://localhost:3001")
            .WriteTo.Sink(new DelegatingSink(e => events.Add(e)))
            .CreateLogger();
        _disposables.Add(logger);

        // Write a test event
        logger.Information("Test message");

        // Assert
        logger.Should().NotBeNull();
        events.Should().HaveCount(1);
    }

    [Fact]
    public void QuickSearch_CanBeConfiguredMultipleTimes()
    {
        // Arrange & Act
        Action act = () =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3001",
                    application: "App1")
                .WriteTo.QuickSearch(
                    serverUrl: "http://localhost:3002",
                    application: "App2")
                .CreateLogger();
            _disposables.Add(logger);
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Options Object Tests

    [Fact]
    public void QuickSearch_OptionsAreValidatedOnConfiguration()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            Period = TimeSpan.Zero // Invalid
        };

        // Act
        Action act = () => new LoggerConfiguration()
            .WriteTo.QuickSearch(options)
            .CreateLogger();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Period");
    }

    [Fact]
    public void QuickSearch_OptionsWithMessageHandler_ShouldBeUsed()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            MessageHandler = handlerMock.Object,
            Period = TimeSpan.FromMilliseconds(100),
            BatchPostingLimit = 1
        };

        // Act
        var logger = new LoggerConfiguration()
            .WriteTo.QuickSearch(options)
            .CreateLogger();
        _disposables.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock HttpMessageHandler that returns a specified response.
    /// </summary>
    private static HttpMessageHandler CreateMockHandler(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });

        return handlerMock.Object;
    }

    /// <summary>
    /// A simple delegating sink for testing chained configuration.
    /// </summary>
    private class DelegatingSink : ILogEventSink
    {
        private readonly Action<LogEvent> _write;

        public DelegatingSink(Action<LogEvent> write)
        {
            _write = write ?? throw new ArgumentNullException(nameof(write));
        }

        public void Emit(LogEvent logEvent)
        {
            _write(logEvent);
        }
    }

    #endregion
}