// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_a_command_result_is_not_authorized_with_a_reason : given.an_administrative_command_context
{
    int _result;

    void Establish() => RemoveApplicationAnswers(new CommandResult { IsAuthorized = false, AuthorizationFailureReason = "Policy refused" });

    async Task Because() => _result = await RemoveApplication();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_keep_the_reason() => _error.ToString().ShouldContain("Policy refused");
    [Fact] void should_name_the_login_command() => _error.ToString().ShouldContain("cratis chronicle login");
}
