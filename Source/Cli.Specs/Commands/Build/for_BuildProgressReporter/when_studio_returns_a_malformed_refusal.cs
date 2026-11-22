// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildProgressReporter;

public class when_studio_returns_a_malformed_refusal : given.a_builder_client
{
    readonly List<string> _log = [];
    bool _leaseCancelled;
    string _outcome;

    async Task Because()
    {
        var order = given.an_order.Parse();
        order.Document["contractVersion"] = "2";
        var work = new StudioBuilderWork(given.an_order.SliceId, DateTimeOffset.UtcNow.AddMinutes(1), BuildGitInstructions.Offline, order);
        _studio.Answer($"work/{work.SliceId}/progress", HttpStatusCode.BadRequest, "{\"reason\":42}");
        using var lease = new CancellationTokenSource();
        await using var reporter = new BuildProgressReporter(_client, work, lease, BuildRedaction.Patterns, _log.Add);
        reporter.Publish(new("First step"));
        reporter.Publish(new("Next step"));
        await reporter.Complete();
        _leaseCancelled = lease.IsCancellationRequested;
        _outcome = await _client.Report(work.SliceId, new JsonObject { ["kind"] = BuildOutcomes.NothingToDo }, CancellationToken.None);
    }

    [Fact] void should_log_the_unexpected_send_exception() => _log.Single().StartsWith("Build progress was not delivered:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_continue_sending_after_the_malformed_refusal() => _studio.BodiesTo($"work/{given.an_order.SliceId}/progress").Count().ShouldEqual(2);
    [Fact] void should_not_cancel_the_lease() => _leaseCancelled.ShouldBeFalse();
    [Fact] void should_allow_the_build_outcome_to_be_reported() => _outcome.ShouldEqual(BuildOutcomes.NothingToDo);
}
