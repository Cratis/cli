// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_receiver_output : Specification
{
    string _result;
    string _interpret;

    void Because()
    {
        var input = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Api, "http://receiver:5005", "./captures"));
        _result = PrologueExtractorCommand.Build(input, "/project/cratis-prologue.json");
        _interpret = PrologueExtractorCommand.Interpret(input, "/project/cratis-prologue.json");
    }

    [Fact] void should_mount_only_the_configuration() => _result.ShouldEqual("docker run --rm -v \"/project/cratis-prologue.json:/config/cratis-prologue.json:ro\" cratis/prologue-extractor:2.0.3");
    [Fact] void should_preserve_the_existing_interpretation_hint() => _interpret.ShouldEqual("cratis prologue interpret");
}
