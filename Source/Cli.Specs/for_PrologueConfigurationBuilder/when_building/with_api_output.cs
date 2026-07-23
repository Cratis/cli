// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_api_output : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [],
        null,
        null,
        new(OutputKind.Api, "http://receiver:5005", "./captures")));

    [Fact] void should_use_the_api_output_kind() => _result.Prologue.Output.Kind.ShouldEqual(OutputKind.Api);
    [Fact] void should_carry_the_endpoint() => _result.Prologue.Output.Api.Endpoint.ShouldEqual("http://receiver:5005");
}
