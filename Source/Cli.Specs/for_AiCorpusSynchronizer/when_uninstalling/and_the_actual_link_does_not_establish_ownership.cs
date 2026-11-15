// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_the_actual_link_does_not_establish_ownership : given.an_ownership_plan
{
    readonly List<SyncResult> _removals = [];
    readonly List<bool> _preserved = [];

    void Because()
    {
        var path = Path.Combine(_project, ".pi", "skills");
        DeleteLink(path);
        File.WriteAllText(path, "user owned");
        _removals.Add(AiCorpusSynchronizer.Uninstall(_project, force: true));
        _preserved.Add(File.ReadAllText(path) == "user owned");
        File.Delete(path);
        Directory.CreateSymbolicLink(path, "../.cratis/ai/agents");
        _removals.Add(AiCorpusSynchronizer.Uninstall(_project, force: true));
        _preserved.Add(new DirectoryInfo(path).LinkTarget == "../.cratis/ai/agents");
        DeleteLink(path);
        _removals.Add(AiCorpusSynchronizer.Uninstall(_project, force: true));
    }

    [Fact] void should_refuse_regular_files_and_other_registered_targets() => _removals.Take(2).All(result => result.Conflicts.Contains(".pi/skills")).ShouldBeTrue();
    [Fact] void should_preserve_both_unowned_leaves() => _preserved.TrueForAll(value => value).ShouldBeTrue();
    [Fact] void should_allow_a_missing_link_to_be_a_no_op_with_force() => _removals[2].Conflicts.ShouldBeEmpty();
}
