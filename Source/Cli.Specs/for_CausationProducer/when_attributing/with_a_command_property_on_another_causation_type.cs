// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_CausationProducer.when_attributing;

public class with_a_command_property_on_another_causation_type : Specification
{
    ObservedProducer? _producer;

    void Because() => _producer = CausationProducer.From(
    [
        new Causation { Type = "Root" },
        new Causation { Type = "ASP.NET Request", Properties = new Dictionary<string, string> { [CausationProducer.CommandTypeProperty] = "Register" } }
    ]);

    [Fact] void should_not_attribute_to_a_command() => _producer.ShouldBeNull();
}
