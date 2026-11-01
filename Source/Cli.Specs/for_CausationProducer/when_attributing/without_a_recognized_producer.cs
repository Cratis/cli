// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_CausationProducer.when_attributing;

public class without_a_recognized_producer : Specification
{
    ObservedProducer? _producer;

    void Because() => _producer = CausationProducer.From(
    [
        new Causation { Type = "Root" },
        new Causation { Type = "Command", Properties = new Dictionary<string, string> { ["name"] = "value" } },
        new Causation { Type = CausationProducer.ReactorCausationType }
    ]);

    [Fact] void should_leave_the_event_unattributed() => _producer.ShouldBeNull();
}
