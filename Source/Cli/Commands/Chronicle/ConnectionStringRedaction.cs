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

    // Match the SDK's credential-like query keys while preserving the original string even when it cannot be parsed.
    [GeneratedRegex("(?<prefix>[?&][^=&#]*(?:password|secret|token|key|credential)[^=&#]*=)[^&#]*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    static partial Regex SecretOptionRegex { get; }

    /// <summary>
    /// Redacts credentials, tokens and certificate passwords from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to redact.</param>
    /// <returns>The connection string without credential values.</returns>
    internal static string Redact(string connectionString) =>
        SecretOptionRegex.Replace(CredentialsRegex.Replace(connectionString, "://${user}:***@"), "${prefix}***");
}
