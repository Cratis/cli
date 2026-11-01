// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_job_id_is_invalid : given.a_delete_job_command
{
    int _result;

    void Establish() => _settings.JobId = "not-a-guid";

    async Task Because() => _result = await Delete();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_describe_the_invalid_id() => _error.ToString().ShouldContain("must be a valid GUID");
    [Fact] void should_not_call_the_server() => _jobs.DidNotReceive().DeleteJob(Arg.Any<DeleteJobRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
