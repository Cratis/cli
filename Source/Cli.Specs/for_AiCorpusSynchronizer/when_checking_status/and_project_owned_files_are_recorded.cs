// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_project_owned_files_are_recorded : given.an_ownership_plan
{
    readonly List<string> _paths = [];

    void Because()
    {
        foreach (var destination in new[] { "rules/project.md", "rules/project/build.md", "RULES/PROJECT.MD", "Rules/Project/Build.md" })
        {
            var path = Path.Combine(_project, AiCorpusSynchronizer.ManagedRoot, destination);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "# Local instructions");
            _paths.Add(path);
            var entry = new AiManagedFile(destination, destination, AiCorpusSynchronizer.Hash(File.ReadAllText(path)));
            SetManifest($"[{JsonSerializer.Serialize(entry)}]", "[]");
            ExerciseAllCommands();
        }
    }

    [Fact] void should_reject_every_lifecycle_variant() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_reject_before_destination_access() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
    [Fact] void should_preserve_local_instructions() => _paths.TrueForAll(path => File.ReadAllText(path) == "# Local instructions").ShouldBeTrue();
}
