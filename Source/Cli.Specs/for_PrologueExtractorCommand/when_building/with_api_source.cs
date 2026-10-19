// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_api_source : Specification
{
    string _result;

    void Because() => _result = PrologueExtractorCommand.Build(new PrologueWizardInput(
        Guid.NewGuid(), [], [], new("/api", "http://system:5000"), null, new(OutputKind.Json, "", "./captures")), "/project/cratis-prologue.json");

    [Fact] void should_publish_the_reverse_proxy() => _result.ShouldContain("-p 8080:8080");
    [Fact] void should_not_publish_otlp_ports() => _result.ShouldNotContain("4317");
}
