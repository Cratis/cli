// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitInstaller;

public class when_installing_for_claude_code : given.a_kit_and_a_project
{
    BuildProjectSettings _record;

    async Task Because() => _record = await Install("claude");

    [Fact] void should_write_the_instructions_into_claude_md() => Read("CLAUDE.md").ShouldContain("Each slice is src/<Slice>.txt");
    [Fact] void should_open_the_kits_block() => Read("CLAUDE.md").ShouldContain(BuildKitInstructions.Start("shell-demo"));
    [Fact] void should_close_the_kits_block() => Read("CLAUDE.md").ShouldContain(BuildKitInstructions.End("shell-demo"));
    [Fact] void should_name_each_skill_with_its_description() => Read("CLAUDE.md").ShouldContain("- `.claude/skills/build-slice/SKILL.md`: Builds one slice of the shell stack.");
    [Fact] void should_install_the_skills_where_claude_code_reads_them() => File.Exists(Path.Combine(_project, ".claude", "skills", "build-slice", "SKILL.md")).ShouldBeTrue();
    [Fact] void should_not_write_agents_md() => File.Exists(Path.Combine(_project, "AGENTS.md")).ShouldBeFalse();
    [Fact] void should_record_the_kits_name() => Record["name"]!.GetValue<string>().ShouldEqual("shell-demo");
    [Fact] void should_record_the_kits_version() => Record["version"]!.GetValue<string>().ShouldEqual("1.0.0");
    [Fact] void should_record_the_archives_checksum() => Record["sha256"]!.GetValue<string>().ShouldEqual(Checksum);
    [Fact] void should_record_the_agent() => Record["agent"]!.GetValue<string>().ShouldEqual("claude");
    [Fact] void should_record_the_instruction_file_it_wrote() => _record.Files.ShouldContain("CLAUDE.md");
    [Fact] void should_record_the_skills_it_wrote() => _record.Files.ShouldContain(".claude/skills/build-slice/");
    [Fact] void should_read_back_what_it_recorded() => BuildProjectSettings.Read(_project)!.Files.Count.ShouldEqual(_record.Files.Count);
}
