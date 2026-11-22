// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_building_a_semantic_retry_command;

public class with_an_empty_path : Specification
{
    string _command = null!;

    void Because() => _command = new RunSettings { Path = string.Empty }.SemanticCommand();

    [Fact]
    void should_preserve_the_empty_argument() => _command.ShouldEqual(
        OperatingSystem.IsWindows()
            ? "cratis run \"\" --engine semantic"
            : "cratis run '' --engine semantic");
}
