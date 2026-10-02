// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp;

public class when_identity_stalls_after_headers : given.a_stalled_response
{
    Exception _error = null!;

    async Task Because() => _error = await Catch.Exception(() => DirectStatusCommand.GetIdentity(Http, Target, "access", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));

    [Fact] void should_start_reading_the_body() => BodyWasRead.ShouldBeTrue();
    [Fact] void should_cancel_the_stalled_body_read() => BodyWasCanceled.ShouldBeTrue();
    [Fact] void should_report_cancellation_instead_of_hanging() => (_error is OperationCanceledException).ShouldBeTrue();
}
