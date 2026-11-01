// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_open_telemetry_source : Specification
{
    string _result;

    PrologueWizardInput _input;

    void Establish() => _input = new(
        Guid.NewGuid(), [], [], null, new([], [], "", ""), new(OutputKind.Json, "", "./captures"));

    void Because() => _result = PrologueExtractorCommand.Build(_input, "/project/cratis-prologue.json");

    [Fact] void should_publish_both_otlp_ports() => _result.ShouldContain("-p 4317:4317 -p 4318:4318");
    [Fact] void should_not_publish_the_reverse_proxy() => _result.ShouldNotContain("8080");
}
