// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_kernel_accepts_the_deletion : given.a_delete_job_command
{
    int _result;
    JsonElement _response;

    async Task Because()
    {
        _result = await Delete();
        _response = JsonDocument.Parse(_output.ToString()).RootElement.Clone();
    }

    [Fact] void should_report_success() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_send_the_scoped_request_once() => _jobs.Received(1).DeleteJob(Arg.Is<DeleteJobRequest>(_ => _.EventStore == _settings.EventStore && _.Namespace == _settings.Namespace && _.JobId == Guid.Parse(_settings.JobId)), Arg.Any<ProtoBuf.Grpc.CallContext>());
    [Fact] void should_report_only_that_deletion_was_requested() => _response.GetProperty("message").GetString().ShouldEqual($"Deletion requested for job {_settings.JobId}.");
    [Fact] void should_include_the_event_store() => _response.GetProperty("eventStore").GetString().ShouldEqual(_settings.EventStore);
    [Fact] void should_include_the_namespace() => _response.GetProperty("namespace").GetString().ShouldEqual(_settings.Namespace);
    [Fact] void should_include_the_job_id() => _response.GetProperty("jobId").GetString().ShouldEqual(_settings.JobId);
}
