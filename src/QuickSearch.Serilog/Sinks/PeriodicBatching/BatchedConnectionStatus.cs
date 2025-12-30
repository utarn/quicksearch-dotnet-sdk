// Copyright 2013-2020 Serilog Contributors
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

namespace QuickSearch.Serilog.Sinks.PeriodicBatching;

/// <summary>
/// Manages reconnection period and transient fault response for <see cref="PeriodicBatchingSink"/>.
/// During normal operation an object of this type will simply echo the configured batch transmission
/// period. When availability fluctuates, the class tracks the number of failed attempts, each time
/// increasing the interval before reconnection is attempted (up to a set maximum) and at predefined
/// points indicating that either the current batch, or entire waiting queue, should be dropped. This
/// serves two purposes - first, a loaded receiver may need a temporary reduction in traffic while coming
/// back online. Second, the sender needs to account for both bad batches (the first fault response) and
/// also overproduction (the second, queue-dropping response). In combination these should provide a
/// reasonable delivery effort but ultimately protect the sender from memory exhaustion.
/// </summary>
internal class BatchedConnectionStatus
{
    static readonly TimeSpan MinimumBackoffPeriod = TimeSpan.FromSeconds(5);
    static readonly TimeSpan MaximumBackoffInterval = TimeSpan.FromMinutes(10);

    const int FailuresBeforeDroppingBatch = 8;
    const int FailuresBeforeDroppingQueue = 10;

    readonly TimeSpan _period;

    int _failuresSinceSuccessfulBatch;

    /// <summary>
    /// Creates a new batched connection status tracker.
    /// </summary>
    /// <param name="period">The normal batching period.</param>
    public BatchedConnectionStatus(TimeSpan period)
    {
        if (period < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period), "The batching period must be a positive timespan");

        _period = period;
    }

    /// <summary>
    /// Marks a successful batch operation, resetting the failure counter.
    /// </summary>
    public void MarkSuccess()
    {
        _failuresSinceSuccessfulBatch = 0;
    }

    /// <summary>
    /// Marks a failed batch operation, incrementing the failure counter.
    /// </summary>
    public void MarkFailure()
    {
        ++_failuresSinceSuccessfulBatch;
    }

    /// <summary>
    /// Gets the next interval to wait before attempting another batch.
    /// This implements exponential backoff on failures.
    /// </summary>
    public TimeSpan NextInterval
    {
        get
        {
            // Available, and first failure, just try the batch interval
            if (_failuresSinceSuccessfulBatch <= 1) return _period;

            // Second failure, start ramping up the interval - first 2x, then 4x, ...
            var backoffFactor = Math.Pow(2, _failuresSinceSuccessfulBatch - 1);

            // If the period is ridiculously short, give it a boost so we get some
            // visible backoff.
            var backoffPeriod = Math.Max(_period.Ticks, MinimumBackoffPeriod.Ticks);

            // The "ideal" interval
            var backedOff = (long)(backoffPeriod * backoffFactor);

            // Capped to the maximum interval
            var cappedBackoff = Math.Min(MaximumBackoffInterval.Ticks, backedOff);

            // Unless that's shorter than the period, in which case we'll just apply the period
            var actual = Math.Max(_period.Ticks, cappedBackoff);

            return TimeSpan.FromTicks(actual);
        }
    }

    /// <summary>
    /// Gets whether the current batch should be dropped due to repeated failures.
    /// </summary>
    public bool ShouldDropBatch => _failuresSinceSuccessfulBatch >= FailuresBeforeDroppingBatch;

    /// <summary>
    /// Gets whether the entire queue should be dropped due to excessive failures.
    /// </summary>
    public bool ShouldDropQueue => _failuresSinceSuccessfulBatch >= FailuresBeforeDroppingQueue;
}