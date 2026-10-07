// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp;

public class when_identity_body_read_is_canceled_by_the_caller : given.a_stalled_response
{
    readonly given.a_manual_deadline _clock = new();
    Exception _error = null!;

    void Establish() => Http.Timeout = Timeout.InfiniteTimeSpan;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var request = DirectStatusCommand.GetIdentity(Http, Target, "access", cancellation.Token, _clock);
        try
        {
            // Real-time watchdogs detect hangs only; caller cancellation is explicitly signaled after body-read entry.
            await BodyReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
            await cancellation.CancelAsync();
            _error = await Catch.Exception(() => request.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        }
        finally
        {
            await cancellation.CancelAsync();
            await Catch.Exception(() => request.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        }
    }

    [Fact] void should_dispose_the_request_timer() => _clock.TimerWasDisposed.ShouldBeTrue();
    [Fact] void should_cancel_the_stalled_body_read() => BodyWasCanceled.ShouldBeTrue();
    [Fact] void should_report_caller_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
}
