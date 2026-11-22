// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_building_a_semantic_retry_command;

public class with_a_double_quote : Specification
{
    string _command = null!;

    void Because() => _command = new RunSettings { Path = "./model\"s" }.SemanticCommand();

    [Fact]
    void should_preserve_the_embedded_double_quote() => _command.ShouldEqual(
        OperatingSystem.IsWindows()
            ? "cratis run \"./model`\"s\" --engine semantic"
            : "cratis run './model\"s' --engine semantic");
}
