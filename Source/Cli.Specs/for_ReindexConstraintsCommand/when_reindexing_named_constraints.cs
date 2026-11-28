// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReindexConstraintsCommand;

public class when_reindexing_named_constraints : given.a_reindex_context
{
    void Establish()
    {
        StoreContext();
        JobsAre(JobWith(Cratis.Chronicle.Contracts.Jobs.JobStatus.CompletedSuccessfully));
    }

    Task Because() => Run("--event-store", "orders", "--namespace", "West", "--event-sequence", "audit", "UniqueOrderNumber", "closedOrder", "--wait");

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_start_exactly_one_reindex() => _constraintIndexes.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_send_the_names_in_order_and_case() => TheRequest().ConstraintNames.ShouldContainOnly("UniqueOrderNumber", "closedOrder");
    [Fact] void should_send_the_explicit_event_store() => TheRequest().EventStore.ShouldEqual("orders");
    [Fact] void should_send_the_explicit_namespace() => TheRequest().Namespace.ShouldEqual("West");
    [Fact] void should_send_the_explicit_event_sequence() => TheRequest().EventSequenceId.ShouldEqual("audit");
}
