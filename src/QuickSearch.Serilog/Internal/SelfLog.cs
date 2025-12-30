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

namespace QuickSearch.Serilog.Internal;

/// <summary>
/// Internal logging for QuickSearch sink diagnostics.
/// When enabled, writes diagnostic messages to the configured output.
/// </summary>
internal static class SelfLog
{
    static Action<string>? _output;

    /// <summary>
    /// Enables self-logging by providing an output action.
    /// </summary>
    /// <param name="output">The action to invoke with log messages.</param>
    public static void Enable(Action<string> output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Disables self-logging.
    /// </summary>
    public static void Disable()
    {
        _output = null;
    }

    /// <summary>
    /// Writes a formatted message to the self-log output.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public static void WriteLine(string format, params object?[] args)
    {
        var output = _output;
        if (output == null)
            return;

        try
        {
            var message = string.Format(format, args);
            output($"[QuickSearch.Serilog] {DateTime.UtcNow:O} {message}");
        }
        catch
        {
            // Ignore any errors in self-logging
        }
    }

    /// <summary>
    /// Writes a message to the self-log output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteLine(string message)
    {
        var output = _output;
        if (output == null)
            return;

        try
        {
            output($"[QuickSearch.Serilog] {DateTime.UtcNow:O} {message}");
        }
        catch
        {
            // Ignore any errors in self-logging
        }
    }
}