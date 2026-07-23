// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_json_output : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [],
        null,
        null,
        new(OutputKind.Json, "http://localhost:5005", "./my-captures")));

    [Fact] void should_use_the_json_output_kind() => _result.Prologue.Output.Kind.ShouldEqual(OutputKind.Json);
    [Fact] void should_carry_the_directory() => _result.Prologue.Output.Json.Directory.ShouldEqual("./my-captures");
    [Fact] void should_not_enable_open_telemetry() => _result.Prologue.OpenTelemetry.Enabled.ShouldBeFalse();
}
