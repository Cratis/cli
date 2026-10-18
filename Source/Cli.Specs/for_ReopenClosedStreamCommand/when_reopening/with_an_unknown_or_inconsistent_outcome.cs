// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class with_an_unknown_or_inconsistent_outcome : given.a_reopen_closed_stream_command
{
    [Theory]
    [InlineData(false, ReopenStreamScopeError.None)]
    [InlineData(false, (ReopenStreamScopeError)99)]
    [InlineData(true, (ReopenStreamScopeError)99)]
    [InlineData(true, ReopenStreamScopeError.NotCompleted)]
    [InlineData(true, ReopenStreamScopeError.ClosedByEvent)]
    async Task should_fail_without_claiming_a_repair(bool success, ReopenStreamScopeError error)
    {
        _response.Response = new() { IsSuccess = success, Error = error };
        var result = await Execute();
        result.ShouldEqual(ExitCodes.ValidationError);
        _output.ToString().ShouldBeEmpty();
        _error.ToString().ShouldNotBeEmpty();
    }
}
