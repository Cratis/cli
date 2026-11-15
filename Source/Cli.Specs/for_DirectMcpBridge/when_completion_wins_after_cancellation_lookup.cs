// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_completion_wins_after_cancellation_lookup : Specification
{
    readonly TaskCompletionSource _lookedUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenSource _source;
    DirectMcpRequest _request;
    Exception? _error;
    Exception? _disposed;

    void Establish()
    {
        _source = new();
        _request = new(_source);
    }

    async Task Because()
    {
        var cancellation = CancelRetainedEntry();
        await _lookedUp.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _request.Dispose();
        _disposed = Catch.Exception(() => _ = _source.Token);
        _resume.SetResult();
        _error = await Catch.Exception(() => cancellation.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact] void should_have_disposed_the_source_before_resuming_cancellation() => _disposed.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_tolerate_the_completed_entry() => _error.ShouldBeNull();
    [Fact] void should_not_leave_client_cancellation_bookkeeping_on_the_completed_request() => _request.CancelledByClient.ShouldBeFalse();
    [Fact] void should_not_cancel_the_completed_request() => _request.Token.IsCancellationRequested.ShouldBeFalse();

    async Task CancelRetainedEntry()
    {
        // Retain the same entry a successful in-flight lookup hands to cancellation, then let completion win.
        var cancelling = _request;
        _lookedUp.SetResult();
        await _resume.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancelling.Cancel();
    }

    void Destroy() => _request.Dispose();
}
