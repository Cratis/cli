// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Cli;

/// <summary>
/// Turns every way of being asked to stop into cancellation, so a long-running command can shut down what it
/// started instead of leaving it behind. A second signal is left alone, so the process can always be killed the
/// usual way.
/// </summary>
/// <remarks>
/// <see cref="Console.CancelKeyPress"/> only covers a Ctrl+C typed at an attached interactive terminal - it does
/// not fire for a plain <c language="csharp">kill</c> sent by a process manager, an IDE's stop button, or a
/// script, and a container this process started stays up regardless of which of those asked for it to end.
/// <see cref="System.Runtime.InteropServices.PosixSignalRegistration"/> answers all of them the same way,
/// interactive or not, which is what running a container demands: something has to be there to stop it no
/// matter how the request to stop arrived.
/// </remarks>
public sealed class ShutdownSignal : IDisposable
{
    readonly CancellationTokenSource _source;
    readonly PosixSignalRegistration[] _registrations;

    ShutdownSignal(CancellationTokenSource source)
    {
        _source = source;
        _registrations = [.. SignalsToWatch().Select(signal => PosixSignalRegistration.Create(signal, OnSignal))];
    }

    /// <summary>
    /// Gets the token that is cancelled on the first signal, or when the token it was linked to is cancelled.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Starts watching for a request to stop, canceling along with the given token.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to link to.</param>
    /// <returns>The <see cref="ShutdownSignal"/>, which stops watching when disposed.</returns>
    public static ShutdownSignal LinkedTo(CancellationToken cancellationToken) =>
        new(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }

        _source.Dispose();
    }

    /// <summary>
    /// The signals that ask this process to stop.
    /// </summary>
    /// <returns>SIGINT and SIGTERM everywhere; SIGHUP as well wherever a terminal can hang up.</returns>
    /// <remarks>
    /// Internal rather than private so the platform-dependent part of this class - which signal is watched on
    /// which platform - can be asserted without registering a real one, which specifications cannot do
    /// deterministically or without taking down the process running them.
    /// </remarks>
    internal static IEnumerable<PosixSignal> SignalsToWatch()
    {
        yield return PosixSignal.SIGINT;
        yield return PosixSignal.SIGTERM;

        // Windows has no terminal to hang up, and registering for it there throws.
        if (!OperatingSystem.IsWindows())
        {
            yield return PosixSignal.SIGHUP;
        }
    }

    void OnSignal(PosixSignalContext context)
    {
        if (_source.IsCancellationRequested)
        {
            // Already shutting down - let this one through, so a command that is stuck can still be killed.
            return;
        }

        context.Cancel = true;
        _source.Cancel();
    }
}
