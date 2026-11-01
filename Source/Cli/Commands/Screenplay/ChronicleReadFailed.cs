// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The exception that is thrown when Chronicle rejects a read a namespace observation depends on.
/// </summary>
/// <param name="operation">The read that failed.</param>
/// <param name="unauthorized">Whether the server refused the read for lack of authorization.</param>
/// <param name="detail">The server's explanation.</param>
public class ChronicleReadFailed(string operation, bool unauthorized, string detail)
    : Exception($"Chronicle refused '{operation}': {detail}")
{
    /// <summary>
    /// Gets a value indicating whether the server refused the read for lack of authorization.
    /// </summary>
    public bool Unauthorized { get; } = unauthorized;
}
