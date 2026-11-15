// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_project_owned_files_are_in_the_corpus : given.an_ownership_plan
{
    void Because()
    {
        foreach (var destination in new[] { "rules/project.md", "rules/project/build.md", "rules/PROJECT.MD", "rules/Project/Build.md" })
        {
            var path = Path.Combine(_corpus, AiCorpusSynchronizer.ManagedRoot, destination);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "# Shared instructions");
            _accesses.Clear();
            var operations = new AiFileOperations(false) { ObserveDestination = (kind, accessed) => _accesses.Add((kind, accessed)) };
            _outcomes.Add((Catch.Exception(() => Execute(5, operations)), _accesses.Count));
            File.Delete(path);
        }
    }

    [Fact] void should_reject_every_desired_project_file() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_reject_before_destination_access() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
    [Fact] void should_preserve_the_manifest() => File.ReadAllText(ManifestPath).ShouldEqual(_originalManifest);
}
