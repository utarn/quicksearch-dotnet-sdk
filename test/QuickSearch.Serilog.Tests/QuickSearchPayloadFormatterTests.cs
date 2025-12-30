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

using System.Text.Json;
using QuickSearch.Serilog.Formatting;
using QuickSearch.Serilog.Tests.Support;
using Serilog.Events;

namespace QuickSearch.Serilog.Tests;

/// <summary>
/// Unit tests for QuickSearchPayloadFormatter JSON serialization.
/// </summary>
public class QuickSearchPayloadFormatterTests
{
    #region Empty and Null Event Tests

    [Fact]
    public void FormatPayload_WithEmptyEventList_ShouldReturnEmptyArray()
    {
        // Arrange
        var events = Array.Empty<LogEvent>();

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", null);

        // Assert
        result.Should().Be("[]");
    }

    [Fact]
    public void FormatPayload_WithSingleEvent_ShouldReturnArrayWithOneElement()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");
        var events = new[] { logEvent };

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", null);

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(1);
    }

    #endregion

    #region Log Level Mapping Tests

    [Theory]
    [InlineData(LogEventLevel.Verbose, "Trace")]
    [InlineData(LogEventLevel.Debug, "Debug")]
    [InlineData(LogEventLevel.Information, "Information")]
    [InlineData(LogEventLevel.Warning, "Warning")]
    [InlineData(LogEventLevel.Error, "Error")]
    [InlineData(LogEventLevel.Fatal, "Critical")]
    public void FormatSingleEvent_ShouldMapLogLevelCorrectly(LogEventLevel level, string expectedType)
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(level, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("type").GetString().Should().Be(expectedType);
        json.GetProperty("data").GetProperty("level").GetString().Should().Be(level.ToString());
    }

    #endregion

    #region Application Name Tests

    [Fact]
    public void FormatSingleEvent_WithApplication_ShouldIncludeApplicationName()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "MyApplication");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("application").GetString().Should().Be("MyApplication");
    }

    [Fact]
    public void FormatSingleEvent_WithNullApplication_ShouldUseUnknown()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, null);

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("application").GetString().Should().Be("unknown");
    }

    [Fact]
    public void FormatSingleEvent_WithEmptyApplication_ShouldUseUnknown()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("application").GetString().Should().Be("unknown");
    }

    [Fact]
    public void FormatSingleEvent_WithWhitespaceApplication_ShouldUseUnknown()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "   ");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("application").GetString().Should().Be("unknown");
    }

    #endregion

    #region Message Template Tests

    [Fact]
    public void FormatSingleEvent_ShouldIncludeRenderedMessage()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["UserId"] = 12345,
            ["Action"] = "Login"
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "User {UserId} performed {Action}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var message = json.GetProperty("message").GetString();
        message.Should().Contain("12345");
        message.Should().Contain("Login");
    }

    [Fact]
    public void FormatSingleEvent_ShouldIncludeMessageTemplate()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Information,
            "User {UserId} performed {Action}");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("messageTemplate").GetString().Should().Be("User {UserId} performed {Action}");
    }

    [Fact]
    public void FormatSingleEvent_SimpleMessage_ShouldRenderCorrectly()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Information,
            "Simple message without placeholders");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("message").GetString().Should().Be("Simple message without placeholders");
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void FormatSingleEvent_ShouldIncludeTimestamp()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var timestamp = json.GetProperty("timestamp").GetString();
        timestamp.Should().NotBeNullOrEmpty();
        
        // Should be ISO 8601 format
        DateTimeOffset.TryParse(timestamp, out var parsed).Should().BeTrue();
    }

    #endregion

    #region Property Serialization Tests

    [Fact]
    public void FormatSingleEvent_WithStringProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["Name"] = "John Doe"
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Hello {Name}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Name").GetString().Should().Be("John Doe");
    }

    [Fact]
    public void FormatSingleEvent_WithIntegerProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["Count"] = 42
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Count is {Count}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Count").GetInt32().Should().Be(42);
    }

    [Fact]
    public void FormatSingleEvent_WithLongProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["BigNumber"] = 9999999999999L
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Number is {BigNumber}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("BigNumber").GetInt64().Should().Be(9999999999999L);
    }

    [Fact]
    public void FormatSingleEvent_WithDoubleProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["Price"] = 19.99
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Price is {Price}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Price").GetDouble().Should().BeApproximately(19.99, 0.001);
    }

    [Fact]
    public void FormatSingleEvent_WithBooleanProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["IsActive"] = true
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Active: {IsActive}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("IsActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void FormatSingleEvent_WithNullProperty_ShouldSerializeAsNull()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["NullValue"] = null
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Value: {NullValue}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("NullValue").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void FormatSingleEvent_WithDateTimeProperty_ShouldSerializeAsIso8601()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        var properties = new Dictionary<string, object?>
        {
            ["EventDate"] = dateTime
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Date: {EventDate}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var dateString = data.GetProperty("EventDate").GetString();
        dateString.Should().Contain("2024-06-15");
    }

    [Fact]
    public void FormatSingleEvent_WithGuidProperty_ShouldSerializeCorrectly()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var properties = new Dictionary<string, object?>
        {
            ["RequestId"] = guid
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Request: {RequestId}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("RequestId").GetString().Should().Be("12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public void FormatSingleEvent_WithArrayProperty_ShouldSerializeAsArray()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Information,
            "Items: {Items}",
            TestHelpers.CreateProperty("Items", new object[] { "apple", "banana", "cherry" }));

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var items = data.GetProperty("Items");
        items.GetArrayLength().Should().Be(3);
        items[0].GetString().Should().Be("apple");
        items[1].GetString().Should().Be("banana");
        items[2].GetString().Should().Be("cherry");
    }

    [Fact]
    public void FormatSingleEvent_WithDictionaryProperty_ShouldSerializeAsObject()
    {
        // Arrange
        var dictValue = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Information,
            "Data: {Data}",
            TestHelpers.CreateProperty("Data", dictValue));

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var dictProp = data.GetProperty("Data");
        dictProp.GetProperty("key1").GetString().Should().Be("value1");
        dictProp.GetProperty("key2").GetString().Should().Be("value2");
    }

    [Fact]
    public void FormatSingleEvent_WithStructureValue_ShouldSerializeCorrectly()
    {
        // Arrange
        var structureValue = TestHelpers.CreateStructureValue(
            "User",
            ("Id", 1),
            ("Name", "John"),
            ("Email", "john@example.com"));
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Information,
            "User: {User}",
            new LogEventProperty("User", structureValue));

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var user = data.GetProperty("User");
        user.GetProperty("$type").GetString().Should().Be("User");
        user.GetProperty("Id").GetInt32().Should().Be(1);
        user.GetProperty("Name").GetString().Should().Be("John");
        user.GetProperty("Email").GetString().Should().Be("john@example.com");
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void FormatSingleEvent_WithException_ShouldIncludeExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");
        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Error,
            "An error occurred",
            exception);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("exception").GetString().Should().Contain("Something went wrong");
        data.GetProperty("exceptionType").GetString().Should().Be("System.InvalidOperationException");
        data.GetProperty("exceptionMessage").GetString().Should().Be("Something went wrong");
    }

    [Fact]
    public void FormatSingleEvent_WithNestedExceptions_ShouldIncludeFullStackTrace()
    {
        // Arrange
        Exception exception;
        try
        {
            try
            {
                throw new ArgumentException("Inner exception");
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Outer exception", inner);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var logEvent = TestHelpers.CreateLogEvent(
            LogEventLevel.Error,
            "An error occurred",
            exception);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var exceptionString = data.GetProperty("exception").GetString();
        exceptionString.Should().Contain("Outer exception");
        exceptionString.Should().Contain("Inner exception");
    }

    [Fact]
    public void FormatSingleEvent_WithoutException_ShouldNotIncludeExceptionFields()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "No error here");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.TryGetProperty("exception", out _).Should().BeFalse();
        data.TryGetProperty("exceptionType", out _).Should().BeFalse();
        data.TryGetProperty("exceptionMessage", out _).Should().BeFalse();
    }

    #endregion

    #region Trace Context Tests

    [Fact]
    public void FormatSingleEvent_WithTraceContext_ShouldIncludeTraceAndSpanIds()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEventWithTraceContext(
            LogEventLevel.Information,
            "Request handled");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        data.TryGetProperty("spanId", out var spanId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrEmpty();
        spanId.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatSingleEvent_WithoutTraceContext_ShouldNotIncludeTraceFields()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "No trace");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.TryGetProperty("traceId", out _).Should().BeFalse();
        data.TryGetProperty("spanId", out _).Should().BeFalse();
    }

    #endregion

    #region Batch Formatting Tests

    [Fact]
    public void FormatPayload_WithMultipleEvents_ShouldReturnValidArray()
    {
        // Arrange
        var events = TestHelpers.CreateLogEventBatch(5).ToList();

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", null);

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(5);
    }

    [Fact]
    public void FormatPayload_WithDifferentLevels_ShouldMapAllCorrectly()
    {
        // Arrange
        var events = TestHelpers.CreateLogEventsWithAllLevels().ToList();

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", null);

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(6);
        
        var types = new List<string>();
        foreach (var element in jsonArray.EnumerateArray())
        {
            types.Add(element.GetProperty("type").GetString()!);
        }
        
        types.Should().Contain("Trace");
        types.Should().Contain("Debug");
        types.Should().Contain("Information");
        types.Should().Contain("Warning");
        types.Should().Contain("Error");
        types.Should().Contain("Critical");
    }

    #endregion

    #region Event Body Limit Tests

    [Fact]
    public void FormatPayload_WithEventExceedingBodyLimit_ShouldDropEvent()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["LargeData"] = new string('x', 10000) // Large string property
        };
        var largeEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Large event {LargeData}",
            properties);
        
        var events = new[] { largeEvent };

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", 100); // Very small limit

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(0); // Event should be dropped
    }

    [Fact]
    public void FormatPayload_WithEventUnderBodyLimit_ShouldIncludeEvent()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Small message");
        var events = new[] { logEvent };

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", 10000); // Large limit

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void FormatPayload_WithNullBodyLimit_ShouldIncludeAllEvents()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["LargeData"] = new string('x', 10000)
        };
        var largeEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Large event {LargeData}",
            properties);
        
        var events = new[] { largeEvent };

        // Act
        var result = QuickSearchPayloadFormatter.FormatPayload(events, "TestApp", null); // No limit

        // Assert
        var jsonArray = JsonDocument.Parse(result).RootElement;
        jsonArray.GetArrayLength().Should().Be(1);
    }

    #endregion

    #region Special Value Tests

    [Fact]
    public void FormatSingleEvent_WithInfinityDouble_ShouldSerializeAsString()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["Value"] = double.PositiveInfinity
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Value: {Value}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert - Should serialize without throwing
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Value").GetString().Should().Be(double.PositiveInfinity.ToString());
    }

    [Fact]
    public void FormatSingleEvent_WithNaN_ShouldSerializeAsString()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["Value"] = double.NaN
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Value: {Value}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Value").GetString().Should().Be(double.NaN.ToString());
    }

    [Fact]
    public void FormatSingleEvent_WithByteArray_ShouldSerializeAsBase64()
    {
        // Arrange
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello" in ASCII
        var properties = new Dictionary<string, object?>
        {
            ["Data"] = bytes
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Data: {Data}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        var base64 = data.GetProperty("Data").GetString();
        base64.Should().Be(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void FormatSingleEvent_WithTimeSpan_ShouldSerializeAsString()
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(2.5);
        var properties = new Dictionary<string, object?>
        {
            ["Duration"] = timeSpan
        };
        var logEvent = TestHelpers.CreateLogEventWithProperties(
            LogEventLevel.Information,
            "Duration: {Duration}",
            properties);

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.GetProperty("Duration").GetString().Should().Be(timeSpan.ToString());
    }

    #endregion

    #region JSON Structure Tests

    [Fact]
    public void FormatSingleEvent_ShouldHaveRequiredTopLevelFields()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("application", out _).Should().BeTrue();
        json.TryGetProperty("timestamp", out _).Should().BeTrue();
        json.TryGetProperty("message", out _).Should().BeTrue();
        json.TryGetProperty("data", out _).Should().BeTrue();
    }

    [Fact]
    public void FormatSingleEvent_DataObject_ShouldContainMessageTemplateAndLevel()
    {
        // Arrange
        var logEvent = TestHelpers.CreateLogEvent(LogEventLevel.Information, "Test message");

        // Act
        var result = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, "TestApp");

        // Assert
        var json = JsonDocument.Parse(result).RootElement;
        var data = json.GetProperty("data");
        data.TryGetProperty("messageTemplate", out _).Should().BeTrue();
        data.TryGetProperty("level", out _).Should().BeTrue();
    }

    #endregion
}