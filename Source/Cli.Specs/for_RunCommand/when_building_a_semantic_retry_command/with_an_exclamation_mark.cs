// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_building_a_semantic_retry_command;

public class with_an_exclamation_mark : Specification
{
    string _command = null!;

    void Because() => _command = new RunSettings { Path = "./models!" }.SemanticCommand();

    [Fact]
    void should_quote_the_path_without_history_expansion() => _command.ShouldEqual(
        OperatingSystem.IsWindows()
            ? "cratis run \"./models!\" --engine semantic"
            : "cratis run './models!' --engine semantic");
}
