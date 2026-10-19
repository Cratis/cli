// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_json_output : Specification
{
    string _result;
    string _interpret;

    void Because()
    {
        var input = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./captures"));
        _result = PrologueExtractorCommand.Build(input, "/project/cratis-prologue.json");
        _interpret = PrologueExtractorCommand.Interpret(input, "/project/cratis-prologue.json");
    }

    [Fact] void should_mount_the_configuration_file_read_only() => _result.ShouldContain("-v \"/project/cratis-prologue.json:/config/cratis-prologue.json:ro\"");
    [Fact] void should_mount_the_host_captures_in_the_container() => _result.ShouldContain($"-v \"/project/captures:{PrologueExtractorCommand.CaptureDirectory}\"");
    [Fact] void should_interpret_the_host_captures() => _interpret.ShouldEqual("cratis prologue interpret \"/project/captures\"");
    [Fact] void should_not_publish_unused_ports() => _result.ShouldNotContain("-p ");
}
