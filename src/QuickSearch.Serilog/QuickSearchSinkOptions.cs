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

namespace QuickSearch.Serilog;

/// <summary>
/// Configuration options for the QuickSearch sink.
/// </summary>
public class QuickSearchSinkOptions
{
    /// <summary>
    /// The base URL of the QuickSearch server (e.g., "http://localhost:3001").
    /// </summary>
    public string ServerUrl { get; set; } = null!;

    /// <summary>
    /// The API key for authentication with the QuickSearch server.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The application name to tag all log events with.
    /// </summary>
    public string? Application { get; set; }

    /// <summary>
    /// The minimum log level for events to be sent to QuickSearch.
    /// </summary>
    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LogEventLevel.Verbose;

    /// <summary>
    /// The maximum number of events to include in a single batch.
    /// Default is 1000.
    /// </summary>
    public int BatchPostingLimit { get; set; } = 1000;

    /// <summary>
    /// The time to wait between checking for event batches.
    /// Default is 2 seconds.
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The maximum number of events to hold in the sink's internal queue.
    /// Default is 100,000.
    /// </summary>
    public int QueueSizeLimit { get; set; } = 100000;

    /// <summary>
    /// The maximum size in bytes for a single event body.
    /// Events larger than this will be dropped. Null means no limit.
    /// </summary>
    public long? EventBodyLimitBytes { get; set; }

    /// <summary>
    /// A custom HTTP message handler for the underlying HttpClient.
    /// Useful for testing or custom proxy configurations.
    /// </summary>
    public HttpMessageHandler? MessageHandler { get; set; }

    /// <summary>
    /// HTTP request timeout. Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
            throw new ArgumentException("ServerUrl is required.", nameof(ServerUrl));

        if (BatchPostingLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(BatchPostingLimit), "BatchPostingLimit must be greater than zero.");

        if (Period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Period), "Period must be greater than zero.");

        if (QueueSizeLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(QueueSizeLimit), "QueueSizeLimit must be greater than zero.");

        if (EventBodyLimitBytes.HasValue && EventBodyLimitBytes.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(EventBodyLimitBytes), "EventBodyLimitBytes must be greater than zero if specified.");
    }
}