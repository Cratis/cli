// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_CausationProducer.when_attributing;

public class with_only_a_reactor : Specification
{
    ObservedProducer? _producer;

    void Because() => _producer = CausationProducer.From(
    [
        new Causation { Type = "Root" },
        new Causation { Type = CausationProducer.ReactorCausationType, Properties = new Dictionary<string, string> { [CausationProducer.ReactorIdProperty] = "Library.Notify" } }
    ]);

    [Fact] void should_attribute_to_the_reactor() => _producer.ShouldEqual(new ObservedProducer(ObservedProducerKind.Reaction, "Library.Notify"));
}
