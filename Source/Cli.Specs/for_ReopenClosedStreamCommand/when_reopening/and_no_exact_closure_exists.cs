// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class and_no_exact_closure_exists : given.a_reopen_closed_stream_command
{
    int _result;

    void Establish() => _response.Response = new() { Error = ReopenStreamScopeError.NotCompleted };

    async Task Because() => _result = await Execute();

    [Fact] void should_fail() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_explain_that_nothing_changed() => _error.ToString().ShouldContain("nothing was reopened");
    [Fact] void should_recommend_inspecting_the_exact_dimensions() => _error.ToString().ShouldContain("all and only its dimensions");
    [Fact] void should_not_report_success() => _output.ToString().ShouldBeEmpty();
}
