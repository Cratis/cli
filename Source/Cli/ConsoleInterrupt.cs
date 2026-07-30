// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Turns Ctrl+C into cancellation so a long-running command can shut down what it started instead of being
/// terminated mid-flight. A second Ctrl+C is left alone, so the process can always be killed the usual way.
/// </summary>
public sealed class ConsoleInterrupt : IDisposable
{
    readonly CancellationTokenSource _source;
    readonly ConsoleCancelEventHandler _handler;

    ConsoleInterrupt(CancellationTokenSource source)
    {
        _source = source;
        _handler = OnCancelKeyPress;
        Console.CancelKeyPress += _handler;
    }

    /// <summary>
    /// Gets the token that is cancelled on the first Ctrl+C, or when the token it was linked to is cancelled.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Starts intercepting Ctrl+C, cancelling along with the given token.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to link to.</param>
    /// <returns>The <see cref="ConsoleInterrupt"/>, which stops intercepting when disposed.</returns>
    public static ConsoleInterrupt LinkedTo(CancellationToken cancellationToken) =>
        new(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

    /// <inheritdoc/>
    public void Dispose()
    {
        Console.CancelKeyPress -= _handler;
        _source.Dispose();
    }

    void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args)
    {
        if (_source.IsCancellationRequested)
        {
            // Already shutting down - let this one through so the process terminates.
            return;
        }

        args.Cancel = true;
        _source.Cancel();
    }
}
