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

using Serilog.Events;

namespace QuickSearch.Serilog.Tests;

/// <summary>
/// Unit tests for QuickSearchSinkOptions configuration and validation.
/// </summary>
public class QuickSearchSinkOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var options = new QuickSearchSinkOptions();

        // Assert
        options.ServerUrl.Should().BeNull();
        options.ApiKey.Should().BeNull();
        options.Application.Should().BeNull();
        options.RestrictedToMinimumLevel.Should().Be(LogEventLevel.Verbose);
        options.BatchPostingLimit.Should().Be(1000);
        options.Period.Should().Be(TimeSpan.FromSeconds(2));
        options.QueueSizeLimit.Should().Be(100000);
        options.EventBodyLimitBytes.Should().BeNull();
        options.MessageHandler.Should().BeNull();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void DefaultValues_RestrictedToMinimumLevel_ShouldBeVerbose()
    {
        // Arrange & Act
        var options = new QuickSearchSinkOptions();

        // Assert
        options.RestrictedToMinimumLevel.Should().Be(LogEventLevel.Verbose);
    }

    [Fact]
    public void DefaultValues_Timeout_ShouldBe30Seconds()
    {
        // Arrange & Act
        var options = new QuickSearchSinkOptions();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Validation Tests - ServerUrl

    [Fact]
    public void Validate_WithNullServerUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = null!
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ServerUrl*")
            .WithParameterName("ServerUrl");
    }

    [Fact]
    public void Validate_WithEmptyServerUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = ""
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ServerUrl*");
    }

    [Fact]
    public void Validate_WithWhitespaceServerUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "   "
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ServerUrl*");
    }

    [Fact]
    public void Validate_WithValidServerUrl_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001"
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validation Tests - BatchPostingLimit

    [Fact]
    public void Validate_WithZeroBatchPostingLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            BatchPostingLimit = 0
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BatchPostingLimit");
    }

    [Fact]
    public void Validate_WithNegativeBatchPostingLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            BatchPostingLimit = -1
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BatchPostingLimit");
    }

    [Fact]
    public void Validate_WithPositiveBatchPostingLimit_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            BatchPostingLimit = 500
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validation Tests - Period

    [Fact]
    public void Validate_WithZeroPeriod_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            Period = TimeSpan.Zero
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Period");
    }

    [Fact]
    public void Validate_WithNegativePeriod_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            Period = TimeSpan.FromSeconds(-1)
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Period");
    }

    [Fact]
    public void Validate_WithPositivePeriod_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            Period = TimeSpan.FromMilliseconds(100)
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validation Tests - QueueSizeLimit

    [Fact]
    public void Validate_WithZeroQueueSizeLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            QueueSizeLimit = 0
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("QueueSizeLimit");
    }

    [Fact]
    public void Validate_WithNegativeQueueSizeLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            QueueSizeLimit = -100
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("QueueSizeLimit");
    }

    [Fact]
    public void Validate_WithPositiveQueueSizeLimit_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            QueueSizeLimit = 50000
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validation Tests - EventBodyLimitBytes

    [Fact]
    public void Validate_WithZeroEventBodyLimitBytes_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            EventBodyLimitBytes = 0
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("EventBodyLimitBytes");
    }

    [Fact]
    public void Validate_WithNegativeEventBodyLimitBytes_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            EventBodyLimitBytes = -1
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("EventBodyLimitBytes");
    }

    [Fact]
    public void Validate_WithPositiveEventBodyLimitBytes_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            EventBodyLimitBytes = 262144 // 256KB
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullEventBodyLimitBytes_ShouldNotThrow()
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            EventBodyLimitBytes = null
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Custom Configuration Tests

    [Fact]
    public void CustomConfiguration_ShouldSetAllProperties()
    {
        // Arrange
        using var handler = new HttpClientHandler();
        
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "https://quicksearch.example.com",
            ApiKey = "my-api-key-12345",
            Application = "MyTestApplication",
            RestrictedToMinimumLevel = LogEventLevel.Warning,
            BatchPostingLimit = 250,
            Period = TimeSpan.FromSeconds(5),
            QueueSizeLimit = 50000,
            EventBodyLimitBytes = 524288, // 512KB
            MessageHandler = handler,
            Timeout = TimeSpan.FromMinutes(1)
        };

        // Assert
        options.ServerUrl.Should().Be("https://quicksearch.example.com");
        options.ApiKey.Should().Be("my-api-key-12345");
        options.Application.Should().Be("MyTestApplication");
        options.RestrictedToMinimumLevel.Should().Be(LogEventLevel.Warning);
        options.BatchPostingLimit.Should().Be(250);
        options.Period.Should().Be(TimeSpan.FromSeconds(5));
        options.QueueSizeLimit.Should().Be(50000);
        options.EventBodyLimitBytes.Should().Be(524288);
        options.MessageHandler.Should().BeSameAs(handler);
        options.Timeout.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void CustomConfiguration_RestrictedToMinimumLevel_ShouldAcceptAllLevels(LogEventLevel level)
    {
        // Arrange
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            RestrictedToMinimumLevel = level
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
        options.RestrictedToMinimumLevel.Should().Be(level);
    }

    [Fact]
    public void CustomConfiguration_WithOptionalApiKey_ShouldBeValid()
    {
        // Arrange - ApiKey is optional
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            ApiKey = null
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CustomConfiguration_WithEmptyApiKey_ShouldBeValid()
    {
        // Arrange - Empty API key should be treated as no API key
        var options = new QuickSearchSinkOptions
        {
            ServerUrl = "http://localhost:3001",
            ApiKey = ""
        };

        // Act
        Action act = () => InvokeValidate(options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to invoke the internal Validate method.
    /// </summary>
    private static void InvokeValidate(QuickSearchSinkOptions options)
    {
        // Use reflection to call internal Validate method
        var validateMethod = typeof(QuickSearchSinkOptions)
            .GetMethod("Validate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (validateMethod == null)
        {
            throw new InvalidOperationException("Could not find Validate method on QuickSearchSinkOptions");
        }

        try
        {
            validateMethod.Invoke(options, null);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    #endregion
}