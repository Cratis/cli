// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp;

public class when_identity_stalls_after_headers : given.a_stalled_response
{
    readonly given.a_manual_deadline _clock = new();
    Exception _error = null!;

    void Establish()
    {
        // HttpClient's real header-only timer is irrelevant here; use the controlled whole-request deadline.
        Http.Timeout = Timeout.InfiniteTimeSpan;
    }

    async Task Because()
    {
        var request = DirectStatusCommand.GetIdentity(Http, Target, "access", CancellationToken.None, _clock);
        try
        {
            // Real-time watchdogs detect hangs only; the request deadline expires explicitly after this signal.
            await BodyReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
            _clock.Expire();
            _error = await Catch.Exception(() => request.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        }
        finally
        {
            _clock.Expire();
            await Catch.Exception(() => request.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        }
    }

    [Fact] void should_bound_the_complete_request_to_fifteen_seconds() => _clock.DueTime.ShouldEqual(TimeSpan.FromSeconds(15));
    [Fact] void should_dispose_the_request_timer() => _clock.TimerWasDisposed.ShouldBeTrue();
    [Fact] void should_start_reading_the_body() => BodyWasRead.ShouldBeTrue();
    [Fact] void should_cancel_the_stalled_body_read() => BodyWasCanceled.ShouldBeTrue();
    [Fact] void should_report_cancellation_instead_of_hanging() => (_error is OperationCanceledException).ShouldBeTrue();
}
