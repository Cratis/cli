// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_spaces_and_a_custom_configuration_name : Specification
{
    string _result;
    string _interpret;

    void Because()
    {
        var input = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./my captures"));
        _result = PrologueExtractorCommand.Build(input, "/my project/custom config.json");
        _interpret = PrologueExtractorCommand.Interpret(input, "/my project/custom config.json");
    }

    [Fact] void should_quote_the_custom_configuration_mount() => _result.ShouldContain("-v \"/my project/custom config.json:/config/cratis-prologue.json:ro\"");
    [Fact] void should_quote_the_capture_mount() => _result.ShouldContain("-v \"/my project/my captures:/captures\"");
    [Fact] void should_quote_the_host_path_for_interpretation() => _interpret.ShouldEqual("cratis prologue interpret \"/my project/my captures\"");
}
