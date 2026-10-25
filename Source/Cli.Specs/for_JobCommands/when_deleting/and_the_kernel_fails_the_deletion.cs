// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_kernel_fails_the_deletion : given.a_delete_job_command
{
    int _result;

    void Establish() => DeleteAnswers(new CommandResult { ExceptionMessages = ["Deletion could not be persisted"] });

    async Task Because() => _result = await Delete();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_surface_the_server_failure() => _error.ToString().ShouldContain("Deletion could not be persisted");
    [Fact] void should_not_report_success() => _output.ToString().ShouldBeEmpty();
}
