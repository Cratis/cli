// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitInstaller;

public class when_installing_for_gemini : given.a_kit_and_a_project
{
    Task Because() => Install("gemini");

    [Fact] void should_write_the_instructions_into_gemini_md() => Read("GEMINI.md").ShouldContain(BuildKitInstructions.Start("shell-demo"));
    [Fact] void should_install_the_skills_in_the_shared_agents_folder() => File.Exists(Path.Combine(_project, ".agents", "skills", "build-slice", "SKILL.md")).ShouldBeTrue();
}
