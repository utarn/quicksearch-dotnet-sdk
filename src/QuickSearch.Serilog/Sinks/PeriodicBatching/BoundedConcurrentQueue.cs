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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace QuickSearch.Serilog.Sinks.PeriodicBatching;

/// <summary>
/// A thread-safe queue with a maximum size limit.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
internal class BoundedConcurrentQueue<T>
{
    const int Unbounded = -1;

    readonly ConcurrentQueue<T> _queue = new();
    readonly int _queueLimit;

    int _counter;

    /// <summary>
    /// Creates a new bounded concurrent queue.
    /// </summary>
    /// <param name="queueLimit">The maximum number of items in the queue, or null for unbounded.</param>
    public BoundedConcurrentQueue(int? queueLimit = null)
    {
        if (queueLimit.HasValue && queueLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(queueLimit), "Queue limit must be positive, or `null` to indicate unbounded.");

        _queueLimit = queueLimit ?? Unbounded;
    }

    /// <summary>
    /// Attempts to dequeue an item from the queue.
    /// </summary>
    /// <param name="item">The dequeued item if successful.</param>
    /// <returns>True if an item was dequeued, false if the queue is empty.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (_queueLimit == Unbounded)
            return _queue.TryDequeue(out item);

        var result = false;
        try
        { }
        finally // prevent state corruption while aborting
        {
            if (_queue.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _counter);
                result = true;
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to enqueue an item to the queue.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <returns>True if the item was enqueued, false if the queue is at capacity.</returns>
    public bool TryEnqueue(T item)
    {
        if (_queueLimit == Unbounded)
        {
            _queue.Enqueue(item);
            return true;
        }

        var result = true;
        try
        { }
        finally
        {
            if (Interlocked.Increment(ref _counter) <= _queueLimit)
            {
                _queue.Enqueue(item);
            }
            else
            {
                Interlocked.Decrement(ref _counter);
                result = false;
            }
        }

        return result;
    }
}