// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_an_existing_opencode_jsonc : given.a_home_and_a_project
{
    void Establish() => Write(ProjectFile("opencode.jsonc"), "{\n  // my settings\n  \"theme\": \"dark\"\n}\n");

    void Because() => _plan = Install(DirectMcpScope.Project, ["opencode"]);

    [Fact] void should_register_in_the_jsonc_file() => File.ReadAllText(ProjectFile("opencode.jsonc")).ShouldContain("\"cratis-direct\"");
    [Fact] void should_keep_the_comments() => File.ReadAllText(ProjectFile("opencode.jsonc")).ShouldContain("// my settings");
    [Fact] void should_not_create_an_opencode_json() => File.Exists(ProjectFile("opencode.json")).ShouldBeFalse();
}
