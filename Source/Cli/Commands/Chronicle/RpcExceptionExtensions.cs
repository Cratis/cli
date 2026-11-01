// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Extension methods for <see cref="RpcException"/>.
/// </summary>
public static class RpcExceptionExtensions
{
    /// <summary>
    /// Checks whether the call was rejected with HTTP 401, the transport's answer to a missing, expired or rejected token.
    /// Chronicle commands refresh a cached token or ask for a new login on it, so it is not a refused read.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True when the server answered with HTTP 401.</returns>
    public static bool IsHttpUnauthorized(this RpcException exception) =>
        exception.Status.Detail.Contains("HTTP status code: 401", StringComparison.Ordinal);
}
