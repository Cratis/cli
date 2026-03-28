// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Jobs.when_getting_nonexistent_job.context;

namespace Cratis.Cli.Integration.Chronicle.for_Jobs;

[Collection(ChronicleCollection.Name)]
public class when_getting_nonexistent_job(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync(
            "chronicle",
            "jobs",
            "get",
            "00000000-0000-0000-0000-000000000099",
            "--event-store",
            "system");
    }

    [Fact] void should_return_not_found_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.NotFound);

    [Fact] void should_report_not_found_on_stderr() => Context.Result.StandardError.ShouldContain("not_found");
}
