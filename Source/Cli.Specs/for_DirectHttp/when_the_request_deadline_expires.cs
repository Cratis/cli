// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp;

public class when_the_request_deadline_expires
{
    [Theory]
    [InlineData(100, 100)]
    [InlineData(30000, 15000)]
    [InlineData(-1, 15000)]
    public void should_cancel_at_the_shorter_http_timeout_or_fifteen_second_cap_and_release_the_timer(int httpMilliseconds, int expectedMilliseconds)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(httpMilliseconds) };
        var clock = new given.a_manual_deadline();
        using var caller = new CancellationTokenSource();
        using (var deadline = DirectHttp.Deadline(http, caller.Token, clock))
        {
            deadline.Token.IsCancellationRequested.ShouldBeFalse();
            clock.DueTime.ShouldEqual(TimeSpan.FromMilliseconds(expectedMilliseconds));
            clock.Expire();
            deadline.Token.IsCancellationRequested.ShouldBeTrue();
            caller.IsCancellationRequested.ShouldBeFalse();
        }

        clock.TimerWasDisposed.ShouldBeTrue();
    }
}
