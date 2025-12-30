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

namespace QuickSearch.Serilog.Sinks;

/// <summary>
/// Provides constants and utilities for the QuickSearch API.
/// </summary>
internal static class QuickSearchApi
{
    /// <summary>
    /// The endpoint for posting log events.
    /// </summary>
    public const string EventsResource = "api/events";

    /// <summary>
    /// The content type for JSON requests.
    /// </summary>
    public const string JsonContentType = "application/json";

    /// <summary>
    /// The authorization header name.
    /// </summary>
    public const string AuthorizationHeaderName = "Authorization";

    /// <summary>
    /// The bearer token prefix.
    /// </summary>
    public const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Normalizes a server URL to ensure it ends with a trailing slash.
    /// </summary>
    /// <param name="serverUrl">The server URL to normalize.</param>
    /// <returns>The normalized URL with a trailing slash.</returns>
    public static string NormalizeServerBaseAddress(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("Server URL cannot be null or empty.", nameof(serverUrl));

        var baseUri = serverUrl.Trim();
        if (!baseUri.EndsWith("/"))
            baseUri += "/";
        return baseUri;
    }
}