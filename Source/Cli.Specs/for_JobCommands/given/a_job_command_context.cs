// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Cli.Commands.Chronicle.Jobs;

namespace Cratis.Cli.for_JobCommands.given;

[Collection(CliSpecsCollection.Name)]
public class a_job_command_context : Specification
{
    protected IServices _services;
    protected IJobs _jobs;
    protected JobCommandSettings _settings;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;

    void Establish()
    {
        _jobs = Substitute.For<IJobs>();
        _services = Substitute.For<IServices>();
        _services.Jobs.Returns(_jobs);
        _settings = new JobCommandSettings
        {
            EventStore = "the-event-store",
            Namespace = "the-namespace",
            JobId = Guid.NewGuid().ToString()
        };
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
    }

    protected void StopAnswers(bool accepted, JobCommandRefusal refusal) =>
        _jobs
            .StopJobWithResult(Arg.Any<StopJobWithResultRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new Cratis.Chronicle.Contracts.Commands.CommandResult<StopJobWithResultResponse>
            {
                Response = new StopJobWithResultResponse { Accepted = accepted, Refusal = refusal }
            });

    protected void ResumeAnswers(bool accepted, JobCommandRefusal refusal) =>
        _jobs
            .ResumeJobWithResult(Arg.Any<ResumeJobWithResultRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new Cratis.Chronicle.Contracts.Commands.CommandResult<ResumeJobWithResultResponse>
            {
                Response = new ResumeJobWithResultResponse { Accepted = accepted, Refusal = refusal }
            });

    protected Task<int> Stop() => new StopJobCommandForSpecs().Execute(_services, _settings);

    protected Task<int> Resume() => new ResumeJobCommandForSpecs().Execute(_services, _settings);

    public class StopJobCommandForSpecs : StopJobCommand
    {
        public Task<int> Execute(IServices services, JobCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }

    public class ResumeJobCommandForSpecs : ResumeJobCommand
    {
        public Task<int> Execute(IServices services, JobCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");
    }
}
