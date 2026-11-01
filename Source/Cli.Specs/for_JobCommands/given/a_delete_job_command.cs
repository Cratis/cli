// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Cli.Commands.Chronicle.Jobs;

namespace Cratis.Cli.for_JobCommands.given;

public class a_delete_job_command : a_job_command_context
{
    protected DeleteJobCommandForSpecs _command;

    void Establish()
    {
        _command = new DeleteJobCommandForSpecs();
        DeleteAnswers(new CommandResult());
    }

    protected void DeleteAnswers(CommandResult result) =>
        _jobs.DeleteJob(Arg.Any<DeleteJobRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(result);

    protected Task<int> Delete() => _command.Execute(_services, _settings);

    public class DeleteJobCommandForSpecs : DeleteJobCommand
    {
        public Task<int> Execute(IServices services, JobCommandSettings settings) =>
            ExecuteCommandAsync(services, settings, "json");

        public Task<int> ExecuteBeforeConnecting(JobCommandSettings settings) =>
            ExecuteAsync(null!, settings, CancellationToken.None);

        public string Confirmation(JobCommandSettings settings) => GetConfirmationPrompt(settings);
    }
}
