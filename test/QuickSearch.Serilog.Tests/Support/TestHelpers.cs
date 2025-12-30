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

using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;

namespace QuickSearch.Serilog.Tests.Support;

/// <summary>
/// Helper methods for creating test log events and configurations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a simple LogEvent for testing purposes.
    /// </summary>
    public static LogEvent CreateLogEvent(
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "Test message",
        Exception? exception = null)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        
        return new LogEvent(
            timestamp: DateTimeOffset.UtcNow,
            level: level,
            exception: exception,
            messageTemplate: template,
            properties: Array.Empty<LogEventProperty>());
    }

    /// <summary>
    /// Creates a LogEvent with specific properties for testing purposes.
    /// </summary>
    public static LogEvent CreateLogEvent(
        LogEventLevel level,
        string messageTemplate,
        params LogEventProperty[] properties)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        
        return new LogEvent(
            timestamp: DateTimeOffset.UtcNow,
            level: level,
            exception: null,
            messageTemplate: template,
            properties: properties);
    }

    /// <summary>
    /// Creates a LogEvent with structured properties.
    /// </summary>
    public static LogEvent CreateLogEventWithProperties(
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "User {UserId} performed {Action}",
        Dictionary<string, object?>? propertyValues = null)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        
        var properties = new List<LogEventProperty>();
        if (propertyValues != null)
        {
            foreach (var kvp in propertyValues)
            {
                properties.Add(CreateProperty(kvp.Key, kvp.Value));
            }
        }

        return new LogEvent(
            timestamp: DateTimeOffset.UtcNow,
            level: level,
            exception: null,
            messageTemplate: template,
            properties: properties);
    }

    /// <summary>
    /// Creates a LogEvent with trace context.
    /// </summary>
    public static LogEvent CreateLogEventWithTraceContext(
        LogEventLevel level = LogEventLevel.Information,
        string messageTemplate = "Test message with trace")
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);
        
        // Create an ActivityTraceId and ActivitySpanId for testing
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        return new LogEvent(
            timestamp: DateTimeOffset.UtcNow,
            level: level,
            exception: null,
            messageTemplate: template,
            properties: Array.Empty<LogEventProperty>(),
            traceId: traceId,
            spanId: spanId);
    }

    /// <summary>
    /// Creates a LogEventProperty from a key-value pair.
    /// </summary>
    public static LogEventProperty CreateProperty(string name, object? value)
    {
        return new LogEventProperty(name, CreatePropertyValue(value));
    }

    /// <summary>
    /// Creates a LogEventPropertyValue from an object.
    /// </summary>
    public static LogEventPropertyValue CreatePropertyValue(object? value)
    {
        return value switch
        {
            null => new ScalarValue(null),
            string s => new ScalarValue(s),
            int i => new ScalarValue(i),
            long l => new ScalarValue(l),
            double d => new ScalarValue(d),
            float f => new ScalarValue(f),
            decimal m => new ScalarValue(m),
            bool b => new ScalarValue(b),
            DateTime dt => new ScalarValue(dt),
            DateTimeOffset dto => new ScalarValue(dto),
            Guid g => new ScalarValue(g),
            TimeSpan ts => new ScalarValue(ts),
            byte[] bytes => new ScalarValue(bytes),
            IEnumerable<object> enumerable => CreateSequenceValue(enumerable),
            IDictionary<string, object> dict => CreateDictionaryValue(dict),
            _ => new ScalarValue(value.ToString())
        };
    }

    private static SequenceValue CreateSequenceValue(IEnumerable<object> values)
    {
        return new SequenceValue(values.Select(CreatePropertyValue));
    }

    private static DictionaryValue CreateDictionaryValue(IDictionary<string, object> dict)
    {
        var elements = dict.Select(kvp => 
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                new ScalarValue(kvp.Key), 
                CreatePropertyValue(kvp.Value)));
        return new DictionaryValue(elements);
    }

    /// <summary>
    /// Creates a StructureValue for testing complex objects.
    /// </summary>
    public static StructureValue CreateStructureValue(
        string? typeTag,
        params (string Name, object? Value)[] properties)
    {
        var props = properties
            .Select(p => new LogEventProperty(p.Name, CreatePropertyValue(p.Value)))
            .ToList();
        return new StructureValue(props, typeTag);
    }

    /// <summary>
    /// Creates a batch of log events for testing.
    /// </summary>
    public static IEnumerable<LogEvent> CreateLogEventBatch(int count, LogEventLevel level = LogEventLevel.Information)
    {
        for (int i = 0; i < count; i++)
        {
            yield return CreateLogEvent(level, $"Test message {i + 1}");
        }
    }

    /// <summary>
    /// Creates log events with different levels.
    /// </summary>
    public static IEnumerable<LogEvent> CreateLogEventsWithAllLevels()
    {
        yield return CreateLogEvent(LogEventLevel.Verbose, "Verbose message");
        yield return CreateLogEvent(LogEventLevel.Debug, "Debug message");
        yield return CreateLogEvent(LogEventLevel.Information, "Information message");
        yield return CreateLogEvent(LogEventLevel.Warning, "Warning message");
        yield return CreateLogEvent(LogEventLevel.Error, "Error message");
        yield return CreateLogEvent(LogEventLevel.Fatal, "Fatal message");
    }

    /// <summary>
    /// Gets the test server URL from environment or default.
    /// </summary>
    public static string GetTestServerUrl()
    {
        return Environment.GetEnvironmentVariable("QUICKSEARCH_TEST_SERVER_URL") 
            ?? "http://localhost:3001";
    }

    /// <summary>
    /// Gets the test API key from environment or default.
    /// </summary>
    public static string GetTestApiKey()
    {
        return Environment.GetEnvironmentVariable("QUICKSEARCH_TEST_API_KEY") 
            ?? "f58d7a839c55869dcaac26ff57177ec81478b9adca47e30142bf0e9c7207be6e";
    }
}