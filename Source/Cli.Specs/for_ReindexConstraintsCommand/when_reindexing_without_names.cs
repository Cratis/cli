// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_reindexing_without_names : given.a_reindex_context
{
    void Establish() => StoreContext();

    Task Because() => Run();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_start_exactly_one_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_send_an_empty_name_selection() => TheRequest().ConstraintNames.ShouldBeEmpty();
    [Fact] void should_use_the_default_event_sequence() => TheRequest().EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_use_the_context_event_store() => TheRequest().EventStore.ShouldEqual("context-store");
    [Fact] void should_use_the_context_namespace() => TheRequest().Namespace.ShouldEqual("context-namespace");
    [Fact] void should_announce_the_job_id() => _output.ToString().ShouldContain(_jobId.ToString());
    [Fact] void should_not_query_jobs() => _jobs.ReceivedCalls().ShouldBeEmpty();
}
