// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Removes credentials from Chronicle connection strings before they are displayed.
/// </summary>
internal static partial class ConnectionStringRedaction
{
    [GeneratedRegex("://(?<user>[^:@/]+):[^@/]+@", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    static partial Regex CredentialsRegex { get; }

    [GeneratedRegex("(?<prefix>[?&]apiKey=)[^&#]*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    static partial Regex ApiKeyRegex { get; }

    /// <summary>
    /// Redacts client secrets and access tokens from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to redact.</param>
    /// <returns>The connection string without credential values.</returns>
    internal static string Redact(string connectionString) =>
        ApiKeyRegex.Replace(CredentialsRegex.Replace(connectionString, "://${user}:***@"), "${prefix}***");
}
