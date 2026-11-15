// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_failure_is_another_error : given.an_administrative_command_context
{
    int _result;

    void Establish()
    {
        var failure = new CommandResult();
        failure.ExceptionMessages.Add("User 'alice' already exists");
        AddUserAnswers(failure);
    }

    async Task Because() => _result = await AddUser();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_surface_the_message() => _error.ToString().ShouldContain("already exists");
    [Fact] void should_not_mention_the_login() => _error.ToString().ShouldNotContain("login");
}
