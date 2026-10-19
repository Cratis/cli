// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_api_and_open_telemetry_sources : Specification
{
    string _result;

    void Because() => _result = PrologueExtractorCommand.Build(new PrologueWizardInput(
        Guid.NewGuid(), [], [], new("/api", "http://system:5000"), new([], [], "", ""), new(OutputKind.Json, "", "./captures")), "/project/cratis-prologue.json");

    [Fact] void should_publish_proxy_and_otlp_ports() => _result.ShouldContain("-p 8080:8080 -p 4317:4317 -p 4318:4318");
}
