// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Cli.Commands.Build;

/// <summary>A bounded, ordered, best-effort sender owned by one assignment, never by an ACP read loop.</summary>
public sealed class BuildProgressReporter : IAsyncDisposable
{
    readonly Channel<BuildProgress> _pending = Channel.CreateBounded<BuildProgress>(new BoundedChannelOptions(64) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    readonly CancellationTokenSource _stop;
    readonly Task _sending;
    readonly StudioBuilderWork _work;
    readonly Action<string> _log;
    readonly object _gate = new();
    string? _lastStep;
    bool _closed;

    /// <summary>Starts a sender for one lease.</summary>
    /// <param name="client">The authenticated Studio client.</param>
    /// <param name="work">The assignment, including its endpoint identifier.</param>
    /// <param name="lease">Cancelled when progress or heartbeat loses the lease.</param>
    /// <param name="redaction">The host's credential redactor.</param>
    /// <param name="log">Local diagnostics.</param>
    public BuildProgressReporter(StudioBuilderClient client, StudioBuilderWork work, CancellationTokenSource lease, BuildRedaction redaction, Action<string> log)
    {
        _work = work;
        _log = log;
        _stop = CancellationTokenSource.CreateLinkedTokenSource(lease.Token);
        _sending = Send(client, lease, redaction);
    }

    /// <summary>Queues progress without waiting for network I/O. Calls after completion are ignored.</summary>
    /// <param name="progress">New activity.</param>
    public void Publish(BuildProgress progress)
    {
        if (!BuildProgress.Supports(_work.Order.ContractVersion) || !progress.HasContent) return;
        lock (_gate)
        {
            if (_closed) return;
            if (progress.Log is null && progress.Usage is null && progress.Step == _lastStep) return;
            if (_pending.Writer.TryWrite(progress)) _lastStep = progress.Step;
            else _log("Build progress queue is full; dropped an update.");
        }
    }

    /// <summary>Drains while the lease is live, with a bounded total completion deadline.</summary>
    /// <returns>The drain task.</returns>
    public async Task Complete()
    {
        lock (_gate)
        {
            _closed = true;
            _pending.Writer.TryComplete();
        }

        _stop.CancelAfter(TimeSpan.FromSeconds(3));
        await _sending;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Complete();
        _stop.Dispose();
    }

    async Task Send(StudioBuilderClient client, CancellationTokenSource lease, BuildRedaction redaction)
    {
        try
        {
            await foreach (var progress in _pending.Reader.ReadAllAsync(_stop.Token))
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                deadline.CancelAfter(TimeSpan.FromSeconds(2));
                try
                {
                    if (!await client.Progress(_work, progress, redaction, deadline.Token))
                    {
                        await lease.CancelAsync();
                        return;
                    }
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    _log(redaction.Redact($"Build progress was not delivered: {exception.Message}"));
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
    }
}
