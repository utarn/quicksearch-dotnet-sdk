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

using System.Text;
using System.Text.Json;
using QuickSearch.Serilog.Internal;
using Serilog.Events;

namespace QuickSearch.Serilog.Formatting;

/// <summary>
/// Formats log events into JSON payloads for the QuickSearch API.
/// </summary>
internal static class QuickSearchPayloadFormatter
{
    /// <summary>
    /// Formats a batch of log events into a JSON array payload for the QuickSearch API.
    /// </summary>
    /// <param name="events">The log events to format.</param>
    /// <param name="application">The application name to include in each event.</param>
    /// <param name="eventBodyLimitBytes">Optional maximum size per event in bytes.</param>
    /// <returns>A JSON string containing all events.</returns>
    public static string FormatPayload(
        IEnumerable<LogEvent> events,
        string? application,
        long? eventBodyLimitBytes)
    {
        var eventsList = events.ToList();
        if (eventsList.Count == 0)
            return "[]";

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartArray();

        foreach (var logEvent in eventsList)
        {
            try
            {
                var eventJson = FormatSingleEvent(logEvent, application);
                
                if (eventBodyLimitBytes.HasValue && 
                    Encoding.UTF8.GetByteCount(eventJson) > eventBodyLimitBytes.Value)
                {
                    SelfLog.WriteLine(
                        "Event JSON exceeds the byte size limit of {0} and will be dropped",
                        eventBodyLimitBytes.Value);
                    continue;
                }

                writer.WriteRawValue(eventJson);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(
                    "Event at {0} with message template {1} could not be formatted and will be dropped: {2}",
                    logEvent.Timestamp.ToString("o"),
                    logEvent.MessageTemplate.Text,
                    ex);
            }
        }

        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Formats a single log event into a JSON string.
    /// </summary>
    /// <param name="logEvent">The log event to format.</param>
    /// <param name="application">The application name to include.</param>
    /// <returns>A JSON string representing the event.</returns>
    public static string FormatSingleEvent(LogEvent logEvent, string? application)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();

        // type - map from log level
        writer.WriteString("type", MapLogLevel(logEvent.Level));

        // application
        if (!string.IsNullOrWhiteSpace(application))
        {
            writer.WriteString("application", application);
        }
        else
        {
            writer.WriteString("application", "unknown");
        }

        // timestamp
        writer.WriteString("timestamp", logEvent.Timestamp.UtcDateTime.ToString("O"));

        // message - render the message template with properties
        writer.WriteString("message", logEvent.RenderMessage());

        // data object - contains all properties, message template, exception, trace info
        writer.WriteStartObject("data");

        // Include the message template
        writer.WriteString("messageTemplate", logEvent.MessageTemplate.Text);

        // Include log level as string
        writer.WriteString("level", logEvent.Level.ToString());

        // Include exception if present
        if (logEvent.Exception != null)
        {
            writer.WriteString("exception", logEvent.Exception.ToString());
            writer.WriteString("exceptionType", logEvent.Exception.GetType().FullName);
            writer.WriteString("exceptionMessage", logEvent.Exception.Message);
        }

        // Include trace and span IDs if present
        if (logEvent.TraceId.HasValue)
        {
            writer.WriteString("traceId", logEvent.TraceId.Value.ToString());
        }

        if (logEvent.SpanId.HasValue)
        {
            writer.WriteString("spanId", logEvent.SpanId.Value.ToString());
        }

        // Include all properties
        foreach (var property in logEvent.Properties)
        {
            WritePropertyValue(writer, property.Key, property.Value);
        }

        writer.WriteEndObject(); // end data

        writer.WriteEndObject(); // end root
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Maps a Serilog LogEventLevel to a QuickSearch event type string.
    /// </summary>
    private static string MapLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "Trace",
            LogEventLevel.Debug => "Debug",
            LogEventLevel.Information => "Information",
            LogEventLevel.Warning => "Warning",
            LogEventLevel.Error => "Error",
            LogEventLevel.Fatal => "Critical",
            _ => "Information"
        };
    }

    /// <summary>
    /// Writes a log event property value to the JSON writer.
    /// </summary>
    private static void WritePropertyValue(Utf8JsonWriter writer, string name, LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                WriteScalarValue(writer, name, scalar);
                break;

            case SequenceValue sequence:
                writer.WriteStartArray(name);
                foreach (var element in sequence.Elements)
                {
                    WritePropertyValueWithoutName(writer, element);
                }
                writer.WriteEndArray();
                break;

            case StructureValue structure:
                writer.WriteStartObject(name);
                if (structure.TypeTag != null)
                {
                    writer.WriteString("$type", structure.TypeTag);
                }
                foreach (var prop in structure.Properties)
                {
                    WritePropertyValue(writer, prop.Name, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case DictionaryValue dictionary:
                writer.WriteStartObject(name);
                foreach (var element in dictionary.Elements)
                {
                    var keyStr = element.Key.Value?.ToString() ?? "null";
                    WritePropertyValue(writer, keyStr, element.Value);
                }
                writer.WriteEndObject();
                break;
        }
    }

    /// <summary>
    /// Writes a property value without a property name (for array elements).
    /// </summary>
    private static void WritePropertyValueWithoutName(Utf8JsonWriter writer, LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                WriteScalarValueWithoutName(writer, scalar);
                break;

            case SequenceValue sequence:
                writer.WriteStartArray();
                foreach (var element in sequence.Elements)
                {
                    WritePropertyValueWithoutName(writer, element);
                }
                writer.WriteEndArray();
                break;

            case StructureValue structure:
                writer.WriteStartObject();
                if (structure.TypeTag != null)
                {
                    writer.WriteString("$type", structure.TypeTag);
                }
                foreach (var prop in structure.Properties)
                {
                    WritePropertyValue(writer, prop.Name, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case DictionaryValue dictionary:
                writer.WriteStartObject();
                foreach (var element in dictionary.Elements)
                {
                    var keyStr = element.Key.Value?.ToString() ?? "null";
                    WritePropertyValue(writer, keyStr, element.Value);
                }
                writer.WriteEndObject();
                break;
        }
    }

    /// <summary>
    /// Writes a scalar value with a property name.
    /// </summary>
    private static void WriteScalarValue(Utf8JsonWriter writer, string name, ScalarValue scalar)
    {
        var value = scalar.Value;

        switch (value)
        {
            case null:
                writer.WriteNull(name);
                break;
            case string s:
                writer.WriteString(name, s);
                break;
            case bool b:
                writer.WriteBoolean(name, b);
                break;
            case int i:
                writer.WriteNumber(name, i);
                break;
            case long l:
                writer.WriteNumber(name, l);
                break;
            case double d:
                if (double.IsInfinity(d) || double.IsNaN(d))
                    writer.WriteString(name, d.ToString());
                else
                    writer.WriteNumber(name, d);
                break;
            case float f:
                if (float.IsInfinity(f) || float.IsNaN(f))
                    writer.WriteString(name, f.ToString());
                else
                    writer.WriteNumber(name, f);
                break;
            case decimal dec:
                writer.WriteNumber(name, dec);
                break;
            case DateTime dt:
                writer.WriteString(name, dt.ToString("O"));
                break;
            case DateTimeOffset dto:
                writer.WriteString(name, dto.ToString("O"));
                break;
            case TimeSpan ts:
                writer.WriteString(name, ts.ToString());
                break;
            case Guid g:
                writer.WriteString(name, g.ToString());
                break;
            case byte[] bytes:
                writer.WriteString(name, Convert.ToBase64String(bytes));
                break;
            default:
                writer.WriteString(name, value.ToString());
                break;
        }
    }

    /// <summary>
    /// Writes a scalar value without a property name (for array elements).
    /// </summary>
    private static void WriteScalarValueWithoutName(Utf8JsonWriter writer, ScalarValue scalar)
    {
        var value = scalar.Value;

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                if (double.IsInfinity(d) || double.IsNaN(d))
                    writer.WriteStringValue(d.ToString());
                else
                    writer.WriteNumberValue(d);
                break;
            case float f:
                if (float.IsInfinity(f) || float.IsNaN(f))
                    writer.WriteStringValue(f.ToString());
                else
                    writer.WriteNumberValue(f);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToString("O"));
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto.ToString("O"));
                break;
            case TimeSpan ts:
                writer.WriteStringValue(ts.ToString());
                break;
            case Guid g:
                writer.WriteStringValue(g.ToString());
                break;
            case byte[] bytes:
                writer.WriteStringValue(Convert.ToBase64String(bytes));
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}