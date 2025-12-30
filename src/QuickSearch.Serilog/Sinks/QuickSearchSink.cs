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

using System.Net.Http.Headers;
using System.Text;
using QuickSearch.Serilog.Formatting;
using QuickSearch.Serilog.Internal;
using QuickSearch.Serilog.Sinks.PeriodicBatching;
using Serilog.Events;

namespace QuickSearch.Serilog.Sinks;

/// <summary>
/// A Serilog sink that sends log events to a QuickSearch server.
/// </summary>
internal sealed class QuickSearchSink : IBatchedLogEventSink, IDisposable
{
    /// <summary>
    /// Default maximum number of events per batch.
    /// </summary>
    public const int DefaultBatchPostingLimit = 1000;

    /// <summary>
    /// Default period between batch sends.
    /// </summary>
    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Default maximum queue size.
    /// </summary>
    public const int DefaultQueueSizeLimit = 100000;

    readonly string? _apiKey;
    readonly string? _application;
    readonly long? _eventBodyLimitBytes;
    readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new QuickSearch sink.
    /// </summary>
    /// <param name="serverUrl">The base URL of the QuickSearch server.</param>
    /// <param name="apiKey">Optional API key for authentication.</param>
    /// <param name="application">Optional application name to tag events.</param>
    /// <param name="eventBodyLimitBytes">Optional maximum size per event in bytes.</param>
    /// <param name="messageHandler">Optional custom HTTP message handler.</param>
    /// <param name="timeout">HTTP request timeout.</param>
    public QuickSearchSink(
        string serverUrl,
        string? apiKey,
        string? application,
        long? eventBodyLimitBytes,
        HttpMessageHandler? messageHandler,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentNullException(nameof(serverUrl));

        _apiKey = apiKey;
        _application = application;
        _eventBodyLimitBytes = eventBodyLimitBytes;

        _httpClient = messageHandler != null
            ? new HttpClient(messageHandler, disposeHandler: false)
            : new HttpClient();

        _httpClient.BaseAddress = new Uri(QuickSearchApi.NormalizeServerBaseAddress(serverUrl));
        _httpClient.Timeout = timeout;
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(QuickSearchApi.JsonContentType));

        // Set authorization header if API key is provided
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Called when there are no events to emit.
    /// </summary>
    public Task OnEmptyBatchAsync()
    {
        // No work needed for empty batches
        return Task.CompletedTask;
    }

    /// <summary>
    /// Emits a batch of log events to the QuickSearch server.
    /// Each event is sent individually as the server expects single event objects, not arrays.
    /// </summary>
    /// <param name="batch">The batch of events to emit.</param>
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var events = batch.ToList();
        if (events.Count == 0)
            return;

        var successCount = 0;
        var failCount = 0;

        foreach (var logEvent in events)
        {
            try
            {
                var payload = QuickSearchPayloadFormatter.FormatSingleEvent(logEvent, _application);

                // Check size limit
                if (_eventBodyLimitBytes.HasValue &&
                    Encoding.UTF8.GetByteCount(payload) > _eventBodyLimitBytes.Value)
                {
                    SelfLog.WriteLine(
                        "Event JSON exceeds the byte size limit of {0} and will be dropped",
                        _eventBodyLimitBytes.Value);
                    failCount++;
                    continue;
                }

                using var content = new StringContent(payload, Encoding.UTF8, QuickSearchApi.JsonContentType);

                var response = await _httpClient.PostAsync(QuickSearchApi.EventsResource, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    SelfLog.WriteLine(
                        "Failed to send event to QuickSearch. Status: {0} ({1}). Response: {2}",
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        responseBody);
                    failCount++;

                    // If we get a 401/403, throw immediately to stop processing
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new HttpRequestException(
                            $"Authentication failed when posting events to QuickSearch: {responseBody}");
                    }
                }
                else
                {
                    successCount++;
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw auth errors
            }
            catch (TaskCanceledException ex)
            {
                SelfLog.WriteLine("Request to QuickSearch timed out: {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while sending event to QuickSearch: {0}", ex);
                failCount++;
            }
        }

        if (successCount > 0)
        {
            SelfLog.WriteLine("Successfully sent {0} events to QuickSearch", successCount);
        }

        if (failCount > 0)
        {
            SelfLog.WriteLine("Failed to send {0} events to QuickSearch", failCount);
        }
    }
}