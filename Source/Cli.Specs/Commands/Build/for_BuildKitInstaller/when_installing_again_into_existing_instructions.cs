// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitInstaller;

public class when_installing_again_into_existing_instructions : given.a_kit_and_a_project
{
    const string Own = "# Our project\n\nAlways write tests first.\n";
    string _instructions;

    void Establish() => File.WriteAllText(Path.Combine(_project, "AGENTS.md"), Own);

    async Task Because()
    {
        await Install("codex", existing: true);
        await File.AppendAllTextAsync(Path.Combine(_project, "AGENTS.md"), "\nA line added after the kit.\n");
        Write("AGENTS.md", "Each slice is now src/<Slice>.md.");
        await Install("codex");
        _instructions = Read("AGENTS.md");
    }

    [Fact] void should_keep_what_the_file_had_before() => _instructions.StartsWith(Own.TrimEnd(), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_what_was_added_after_the_block() => _instructions.ShouldContain("A line added after the kit.");
    [Fact] void should_hold_the_kits_block_once() => _instructions.Split(BuildKitInstructions.Start("shell-demo")).Length.ShouldEqual(2);
    [Fact] void should_replace_the_instructions_in_the_block() => _instructions.ShouldContain("Each slice is now src/<Slice>.md.");
    [Fact] void should_drop_the_earlier_instructions() => _instructions.ShouldNotContain("Each slice is src/<Slice>.txt");
}
