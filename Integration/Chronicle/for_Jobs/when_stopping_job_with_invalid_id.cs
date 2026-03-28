// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Jobs.when_stopping_job_with_invalid_id.context;

namespace Cratis.Cli.Integration.Chronicle.for_Jobs;

[Collection(ChronicleCollection.Name)]
public class when_stopping_job_with_invalid_id(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync(
            "chronicle",
            "jobs",
            "stop",
            "not-a-guid",
            "--event-store",
            "system");
    }

    [Fact] void should_return_validation_error_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ValidationError);

    [Fact] void should_report_validation_error_on_stderr() => Context.Result.StandardError.ShouldContain("validation_error");
}
