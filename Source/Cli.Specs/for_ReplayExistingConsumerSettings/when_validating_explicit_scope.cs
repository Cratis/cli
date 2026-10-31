// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Cli.Registration;

namespace Cratis.Cli.for_ReplayExistingConsumerSettings;

public class when_validating_explicit_scope : Specification
{
    [Theory]
    [InlineData("observer")]
    [InlineData("store")]
    [InlineData("namespace")]
    [InlineData("sequence")]
    [InlineData("subscription")]
    [InlineData("consumer")]
    public void should_reject_each_blank_identifier(string identifier)
    {
        var settings = ValidScope();
        switch (identifier)
        {
            case "observer": settings.ObserverId = " "; break;
            case "store": settings.EventStore = " "; break;
            case "namespace": settings.Namespace = " "; break;
            case "sequence": settings.EventSequenceId = " "; break;
            case "subscription": settings.SubscriptionId = " "; break;
            case "consumer": settings.ConsumerIdentity = " "; break;
        }

        settings.Validate().Successful.ShouldBeFalse();
    }

    [Theory]
    [InlineData("event-log")]
    [InlineData("Outbox")]
    [InlineData("outbox ")]
    public void should_reject_any_sequence_other_than_exact_outbox(string sequence)
    {
        var settings = ValidScope();
        settings.EventSequenceId = sequence;
        settings.Validate().Successful.ShouldBeFalse();
    }

    [Fact] void should_accept_opaque_operator_labels() => ValidScope().Validate().Successful.ShouldBeTrue();
    [Fact] void should_reject_omitted_scope_instead_of_using_context_defaults() => new ReplayExistingConsumerSettings().Validate().Successful.ShouldBeFalse();
    [Fact] void should_not_use_event_store_interception() => typeof(EventStoreSettings).IsAssignableFrom(typeof(ReplayExistingConsumerSettings)).ShouldBeFalse();

    [Fact] void should_not_offer_observer_discovery_completion() => typeof(ReplayExistingConsumerCommand).GetCustomAttribute<CliCommandAttribute>()!.DynamicCompletion.ShouldBeNull();
    [Fact] void should_not_offer_dynamic_scope_completion() => typeof(ReplayExistingConsumerSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).SelectMany(_ => _.GetCustomAttributes<DynamicOptionCompletionAttribute>()).ShouldBeEmpty();

    static ReplayExistingConsumerSettings ValidScope() => new()
    {
        ObserverId = "opaque-observer",
        EventStore = "producer",
        Namespace = "namespace",
        EventSequenceId = "outbox",
        SubscriptionId = "[opaque-subscription]",
        ConsumerIdentity = "[opaque-consumer]"
    };
}
