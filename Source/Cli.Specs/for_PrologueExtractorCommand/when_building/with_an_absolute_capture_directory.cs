// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_an_absolute_capture_directory : Specification
{
    string _result;
    string _interpret;

    void Because()
    {
        var input = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "/host/captures"));
        _result = PrologueExtractorCommand.Build(input, "/project/cratis-prologue.json");
        _interpret = PrologueExtractorCommand.Interpret(input, "/project/cratis-prologue.json");
    }

    [Fact] void should_preserve_the_absolute_host_directory() => _result.ShouldContain("-v \"/host/captures:/captures\"");
    [Fact] void should_interpret_the_absolute_host_directory() => _interpret.ShouldEqual("cratis prologue interpret \"/host/captures\"");
}
