// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// Bounds HTTP operations through response-body consumption, not just response headers.
/// </summary>
internal static class DirectHttp
{
    internal static RequestDeadline Deadline(HttpClient http, CancellationToken cancellationToken, TimeProvider? timeProvider = null)
    {
        var maximum = TimeSpan.FromSeconds(15);
        var timeout = http.Timeout > TimeSpan.Zero && http.Timeout < maximum ? http.Timeout : maximum;
        return new RequestDeadline(timeout, timeProvider ?? TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Owns both the request timer and its linkage to caller cancellation.
    /// </summary>
    internal sealed class RequestDeadline : IDisposable
    {
        readonly CancellationTokenSource _timer;
        readonly CancellationTokenSource _linked;

        internal RequestDeadline(TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
        {
            _timer = new CancellationTokenSource(timeout, timeProvider);
            _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timer.Token);
        }

        internal CancellationToken Token => _linked.Token;

        public void Dispose()
        {
            _linked.Dispose();
            _timer.Dispose();
        }
    }
}
