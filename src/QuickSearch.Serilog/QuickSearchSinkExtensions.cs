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

using QuickSearch.Serilog.Sinks;
using QuickSearch.Serilog.Sinks.PeriodicBatching;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace QuickSearch.Serilog;

/// <summary>
/// Extension methods for configuring the QuickSearch sink.
/// </summary>
public static class QuickSearchSinkExtensions
{
    /// <summary>
    /// Writes log events to a QuickSearch server.
    /// </summary>
    /// <param name="sinkConfiguration">The logger sink configuration.</param>
    /// <param name="serverUrl">The base URL of the QuickSearch server (e.g., "http://localhost:3001").</param>
    /// <param name="apiKey">Optional API key for authentication. Uses Bearer token authentication.</param>
    /// <param name="application">Optional application name to tag all log events.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log level to send to QuickSearch.</param>
    /// <param name="batchPostingLimit">Maximum number of events per batch. Default is 1000.</param>
    /// <param name="period">Time between batch sends. Default is 2 seconds.</param>
    /// <param name="queueSizeLimit">Maximum events to queue before dropping. Default is 100,000.</param>
    /// <param name="eventBodyLimitBytes">Maximum size per event in bytes. Events exceeding this are dropped.</param>
    /// <param name="messageHandler">Custom HTTP message handler for testing or proxy scenarios.</param>
    /// <returns>Logger configuration for chaining.</returns>
    /// <example>
    /// <code>
    /// Log.Logger = new LoggerConfiguration()
    ///     .WriteTo.QuickSearch(
    ///         serverUrl: "http://localhost:3001",
    ///         apiKey: "your-api-key",
    ///         application: "MyApp")
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public static LoggerConfiguration QuickSearch(
        this LoggerSinkConfiguration sinkConfiguration,
        string serverUrl,
        string? apiKey = null,
        string? application = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
        int batchPostingLimit = QuickSearchSink.DefaultBatchPostingLimit,
        TimeSpan? period = null,
        int queueSizeLimit = QuickSearchSink.DefaultQueueSizeLimit,
        long? eventBodyLimitBytes = null,
        HttpMessageHandler? messageHandler = null)
    {
        if (sinkConfiguration == null)
            throw new ArgumentNullException(nameof(sinkConfiguration));

        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentNullException(nameof(serverUrl));

        var effectivePeriod = period ?? QuickSearchSink.DefaultPeriod;

        var options = new QuickSearchSinkOptions
        {
            ServerUrl = serverUrl,
            ApiKey = apiKey,
            Application = application,
            RestrictedToMinimumLevel = restrictedToMinimumLevel,
            BatchPostingLimit = batchPostingLimit,
            Period = effectivePeriod,
            QueueSizeLimit = queueSizeLimit,
            EventBodyLimitBytes = eventBodyLimitBytes,
            MessageHandler = messageHandler
        };

        return QuickSearch(sinkConfiguration, options);
    }

    /// <summary>
    /// Writes log events to a QuickSearch server using the provided options.
    /// </summary>
    /// <param name="sinkConfiguration">The logger sink configuration.</param>
    /// <param name="options">The QuickSearch sink options.</param>
    /// <returns>Logger configuration for chaining.</returns>
    /// <example>
    /// <code>
    /// var options = new QuickSearchSinkOptions
    /// {
    ///     ServerUrl = "http://localhost:3001",
    ///     ApiKey = "your-api-key",
    ///     Application = "MyApp",
    ///     BatchPostingLimit = 500
    /// };
    /// 
    /// Log.Logger = new LoggerConfiguration()
    ///     .WriteTo.QuickSearch(options)
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public static LoggerConfiguration QuickSearch(
        this LoggerSinkConfiguration sinkConfiguration,
        QuickSearchSinkOptions options)
    {
        if (sinkConfiguration == null)
            throw new ArgumentNullException(nameof(sinkConfiguration));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.Validate();

        var quickSearchSink = new QuickSearchSink(
            options.ServerUrl,
            options.ApiKey,
            options.Application,
            options.EventBodyLimitBytes,
            options.MessageHandler,
            options.Timeout);

        var batchingOptions = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = options.BatchPostingLimit,
            Period = options.Period,
            QueueLimit = options.QueueSizeLimit,
            EagerlyEmitFirstEvent = true
        };

        var batchingSink = new PeriodicBatchingSink(quickSearchSink, batchingOptions);

        return sinkConfiguration.Sink(batchingSink, options.RestrictedToMinimumLevel);
    }
}