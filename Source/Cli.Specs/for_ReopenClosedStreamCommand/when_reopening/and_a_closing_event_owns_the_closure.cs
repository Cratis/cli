// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class and_a_closing_event_owns_the_closure : given.a_reopen_closed_stream_command
{
    int _result;

    void Establish() => _response.Response = new() { Error = ReopenStreamScopeError.ClosedByEvent };

    async Task Because() => _result = await Execute();

    [Fact] void should_fail() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_explain_the_event_owned_closure() => _error.ToString().ShouldContain("closed by a closing event");
    [Fact] void should_name_the_supported_reopening_path() => _error.ToString().ShouldContain("ReopenedBy events instead");
    [Fact] void should_not_report_success() => _output.ToString().ShouldBeEmpty();
}
