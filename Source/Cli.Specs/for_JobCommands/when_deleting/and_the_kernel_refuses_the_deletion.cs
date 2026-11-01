// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_kernel_refuses_the_deletion : given.a_delete_job_command
{
    int _result;

    void Establish() => DeleteAnswers(CommandResult.Invalid(Guid.NewGuid(), [new ValidationResult { Message = "The job cannot be deleted" }]));

    async Task Because() => _result = await Delete();

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_surface_the_refusal_reason() => _error.ToString().ShouldContain("The job cannot be deleted");
    [Fact] void should_not_report_success() => _output.ToString().ShouldBeEmpty();
}
