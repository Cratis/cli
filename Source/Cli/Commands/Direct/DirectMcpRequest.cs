// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Owns a forwarded request's cancellation source until completion and active cancellations have finished.</summary>
/// <param name="source">The cancellation source whose ownership is transferred to the request.</param>
internal sealed class DirectMcpRequest(CancellationTokenSource source) : IDisposable
{
    readonly object _gate = new();
    bool _completed;
    volatile bool _cancelledByClient;
    int _cancellations;

    /// <summary>Gets the request's cancellation token.</summary>
    internal CancellationToken Token { get; } = source.Token;

    /// <summary>Gets whether the client cancelled this request before it completed.</summary>
    internal bool CancelledByClient => _cancelledByClient;

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            if (_cancellations == 0)
            {
                source.Dispose();
            }
        }
    }

    /// <summary>Cancels an active request without allowing completion to dispose its source mid-cancellation.</summary>
    /// <returns>A task completing when the cancellation callbacks have finished.</returns>
    internal async Task Cancel()
    {
        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            // Mark the cause before callbacks can stop the request, and reserve the source until they finish.
            _cancelledByClient = true;
            _cancellations++;
        }

        try
        {
            await source.CancelAsync();
        }
        finally
        {
            lock (_gate)
            {
                _cancellations--;
                if (_completed && _cancellations == 0)
                {
                    source.Dispose();
                }
            }
        }
    }
}
