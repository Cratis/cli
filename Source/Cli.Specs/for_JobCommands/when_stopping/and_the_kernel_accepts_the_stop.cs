// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_stopping;

public class and_the_kernel_accepts_the_stop : given.a_job_command_context
{
    int _result;

    void Establish() => StopAnswers(true, JobCommandRefusal.NotRefused);

    async Task Because() => _result = await Stop();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_the_result_returning_rpc() => _jobs.Received(1).StopJobWithResult(Arg.Any<StopJobWithResultRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_the_stop() => _output.ToString().ShouldContain("stopped successfully");
}
