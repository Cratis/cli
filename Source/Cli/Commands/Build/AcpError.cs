// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The exception that is thrown for a JSON-RPC error on an Agent Client Protocol connection.
/// </summary>
/// <param name="code">The JSON-RPC error code.</param>
/// <param name="message">What went wrong.</param>
public sealed class AcpError(int code, string message) : Exception(message)
{
    /// <summary>The code of a method the client does not offer.</summary>
    public const int MethodNotFound = -32601;

    /// <summary>The code the client refuses a request with.</summary>
    public const int Refused = -32000;

    /// <summary>
    /// Gets the JSON-RPC error code.
    /// </summary>
    public int Code { get; } = code;
}
