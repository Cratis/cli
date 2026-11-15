// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_a_later_file_is_noncanonical : given.an_ownership_plan
{
    readonly List<string> _invalidPaths = [];

    void Because()
    {
        foreach (var destination in new[] { "../outside", "rules/../../outside", "/outside", "C:/outside", "C:outside", "//server/share", "\\\\server\\share", "rules\\general.md", "rules/./general.md", "rules//general.md", "", "rules/", "rules/NUL.txt", "rules/COM1", "rules/general.md.", "rules/general.md ", "rules/a:b", "rules/a\n.md" })
        {
            _invalidPaths.Add(destination);
            SetManifest($"[{FileEntry("rules/general.md")},{FileEntry(destination)}]", $"[{LinkEntry(".pi/agents", "../.cratis/ai/agents")}]");
            ExerciseAllCommands();
        }
    }

    [Fact] void should_exercise_every_lifecycle_variant() => _outcomes.Count.ShouldEqual(162);
    [Fact] void should_reject_every_plan() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_name_the_offending_path() => _outcomes.Select((outcome, index) => outcome.Error.Message.Contains($"path: {System.Text.Json.JsonSerializer.Serialize(_invalidPaths[index / 9])}", StringComparison.Ordinal)).All(named => named).ShouldBeTrue();
    [Fact] void should_not_access_any_destination() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
    [Fact] void should_keep_the_installed_payload() => File.Exists(Installed("general.md")).ShouldBeTrue();
}
