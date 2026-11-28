// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Cli.Commands.Chronicle.Constraints;
using Cratis.Cli.given;
using ProtoBuf.Grpc;

namespace Cratis.Cli.for_ReindexConstraintsCommand.given;

[Collection(CliSpecsCollection.Name)]
public class a_reindex_context : a_temp_config_directory
{
    protected static readonly Guid _jobId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    protected IConstraintIndexes _constraintIndexes;
    protected IJobs _jobs;
    protected int _result;
    protected StringWriter _output;
    protected StringWriter _error;
    TextWriter _previousOutput;
    TextWriter _previousError;
    string? _previousNonInteractive;

    void Establish()
    {
        _constraintIndexes = Substitute.For<IConstraintIndexes>();
        _jobs = Substitute.For<IJobs>();
        var services = Substitute.For<IServices>();
        services.ConstraintIndexes.Returns(_constraintIndexes);
        services.Jobs.Returns(_jobs);
        ReindexConstraintsCommandForSpecs.Services = services;

        _constraintIndexes
            .ReindexConstraints(Arg.Any<ReindexConstraintsRequest>(), Arg.Any<CallContext>())
            .Returns(new CommandResult<Guid> { Response = _jobId });

        _previousNonInteractive = Environment.GetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");
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
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, _previousNonInteractive);
        ReindexConstraintsCommandForSpecs.Services = null;
    }

    protected async Task Run(params string[] arguments)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ReindexConstraintsCommandForSpecs>("reindex");
        });
        _result = await app.RunAsync([.. new[] { "reindex", "--server", "chronicle://override:35001", "--yes", "-o", "json" }, .. arguments]);
    }

    protected static void StoreContext() =>
        new CliConfiguration
        {
            ActiveContext = "orders",
            Contexts = new Dictionary<string, CliContext>
            {
                ["orders"] = new() { EventStore = "context-store", Namespace = "context-namespace" }
            }
        }.Save();

    protected void JobsAre(params JobSummaryResponse[] jobs) =>
        _jobs.AllJobs(Arg.Any<AllJobsRequest>(), Arg.Any<CallContext>()).Returns(new QueryResult<IEnumerable<JobSummaryResponse>> { Data = jobs });

    protected static JobSummaryResponse JobWith(JobStatus status) => new() { Id = _jobId, Status = status };

    protected ReindexConstraintsRequest TheRequest() =>
        (ReindexConstraintsRequest)_constraintIndexes.ReceivedCalls().Single().GetArguments()[0]!;

    public class ReindexConstraintsCommandForSpecs : ReindexConstraintsCommand
    {
        public static IServices? Services { get; set; }

        /// <inheritdoc/>
        protected override TimeSpan PollInterval => TimeSpan.FromMilliseconds(1);

        /// <inheritdoc/>
        protected override TimeSpan WaitTimeout => TimeSpan.FromSeconds(5);

        /// <inheritdoc/>
        protected override Task<int> ExecuteConnectedAsync(ChronicleConnectionString connectionString, ReindexConstraintsSettings settings, string format, CancellationToken cancellationToken) =>
            ExecuteCommandAsync(Services!, settings, format);
    }
}
