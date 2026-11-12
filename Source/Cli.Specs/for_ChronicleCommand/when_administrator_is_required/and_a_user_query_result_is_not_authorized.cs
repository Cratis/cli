// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_a_user_query_result_is_not_authorized : given.an_administrative_command_context
{
    int _result;

    void Establish() => AllUsersAnswers(new QueryResult<IEnumerable<UserResponse>> { IsAuthorized = false });

    async Task Because() => _result = await ListUsers();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_name_the_login_command() => _error.ToString().ShouldContain("cratis chronicle login");
    [Fact] void should_say_an_administrator_is_needed() => _error.ToString().ShouldContain("administrator");
    [Fact] void should_not_print_an_empty_list() => _output.ToString().ShouldBeEmpty();
}
