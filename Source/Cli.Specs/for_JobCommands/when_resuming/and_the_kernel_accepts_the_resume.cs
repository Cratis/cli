// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_resuming;

public class and_the_kernel_accepts_the_resume : given.a_job_command_context
{
    int _result;

    void Establish() => ResumeAnswers(true, JobCommandRefusal.NotRefused);

    async Task Because() => _result = await Resume();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_the_result_returning_rpc() => _jobs.Received(1).ResumeJobWithResult(Arg.Any<ResumeJobWithResultRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_resume() => _output.ToString().ShouldContain("resumed successfully");
}
