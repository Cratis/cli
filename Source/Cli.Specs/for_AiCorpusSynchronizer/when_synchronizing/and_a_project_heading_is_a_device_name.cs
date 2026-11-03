// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_project_heading_is_a_device_name : given.an_ownership_plan
{
    static readonly HashSet<string> _mutationKinds = ["write", "directory", "replace", "delete", "link"];
    readonly List<Exception?> _errors = [];
    readonly List<string> _mutations = [];
    bool _partialDirectory;
    SyncResult _retry = null!;
    string LegacyPath => Path.Combine(_project, ".cratis", "PROJECT.md");
    string ConcernsPath => Path.Combine(_project, ".cratis", "ai", "rules", "project");

    void Because()
    {
        foreach (var heading in new[] { "Aux", "Con", "Nul" })
        {
            File.WriteAllText(LegacyPath, $"# Project\n\n## Build\n\nBuild instructions\n\n## {heading}\n\nLocal instructions\n");
            var operations = new AiFileOperations(false)
            {
                ObserveDestination = (kind, path) =>
                {
                    if (_mutationKinds.Contains(kind)) _mutations.Add(path);
                }
            };
            _errors.Add(Catch.Exception(() => AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration, new(false, operations))));
            _partialDirectory |= Directory.Exists(ConcernsPath);
        }
        File.WriteAllText(LegacyPath, "# Project\n\n## Build\n\nBuild instructions\n");
        _retry = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
    }

    [Fact] void should_reject_every_device_heading() => _errors.TrueForAll(error => error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_reject_before_any_write() => _mutations.ShouldBeEmpty();
    [Fact] void should_not_leave_a_partial_concerns_directory() => _partialDirectory.ShouldBeFalse();
    [Fact] void should_allow_a_corrected_retry() => _retry.Conflicts.ShouldBeEmpty();
    [Fact] void should_write_the_index_on_retry() => File.ReadAllText(Path.Combine(_project, AiProjectInstructions.RelativePath)).ShouldContain("[Build](.cratis/ai/rules/project/build.md)");
    [Fact] void should_write_the_concern_on_retry() => File.ReadAllText(Path.Combine(ConcernsPath, "build.md")).ShouldContain("Build instructions");
}
