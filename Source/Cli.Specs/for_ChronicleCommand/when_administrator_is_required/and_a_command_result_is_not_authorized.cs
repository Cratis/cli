// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_a_command_result_is_not_authorized : given.an_administrative_command_context
{
    int _result;

    void Establish() => AddUserAnswers(new CommandResult { IsAuthorized = false });

    async Task Because() => _result = await AddUser();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_say_an_administrator_is_needed() => _error.ToString().ShouldContain("administrator");
    [Fact] void should_name_the_login_command() => _error.ToString().ShouldContain("cratis chronicle login");
    [Fact] void should_not_report_success() => _output.ToString().ShouldBeEmpty();
}
