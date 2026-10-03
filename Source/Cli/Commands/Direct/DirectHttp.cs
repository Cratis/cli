// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Bounds HTTP operations through response-body consumption, not just response headers.
/// </summary>
internal static class DirectHttp
{
    internal static CancellationTokenSource Deadline(HttpClient http, CancellationToken cancellationToken)
    {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var maximum = TimeSpan.FromSeconds(15);
        deadline.CancelAfter(http.Timeout > TimeSpan.Zero && http.Timeout < maximum ? http.Timeout : maximum);
        return deadline;
    }
}
