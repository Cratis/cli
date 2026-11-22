// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_building_a_semantic_retry_command;

public class with_plain_characters : Specification
{
    string _command = null!;

    void Because() => _command = new RunSettings { Path = "./Models-123_@sandbox:latest" }.SemanticCommand();

    [Fact]
    void should_leave_the_path_unquoted() => _command.ShouldEqual("cratis run ./Models-123_@sandbox:latest --engine semantic");
}
