// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ListClosedStreamsCommand.when_listing;

public class and_the_server_rejects_the_query : given.a_list_closed_streams_command
{
    int _result;

    void Establish() => _response.ExceptionMessages.Add("Unable to read closures");

    async Task Because() => _result = await Execute();

    [Fact] void should_fail() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_preserve_the_server_message() => _error.ToString().ShouldContain("Unable to read closures");
    [Fact] void should_not_emit_rows_from_an_unsuccessful_query() => _output.ToString().ShouldBeEmpty();
}
