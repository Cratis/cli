// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_a_named_service_api : Specification
{
    string _result;

    PrologueWizardInput _input;

    void Establish() => _input = new(
        Guid.NewGuid(),
        [],
        [],
        null,
        null,
        new(OutputKind.Json, "", "./captures"),
        [new("orders", [], [], new("/orders", "http://orders:8080/"), [])]);

    void Because() => _result = PrologueExtractorCommand.Build(_input, "/project/cratis-prologue.json");

    [Fact] void should_publish_the_reverse_proxy() => _result.ShouldContain("-p 8080:8080");
}
