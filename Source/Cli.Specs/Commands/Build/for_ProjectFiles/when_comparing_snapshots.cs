// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_ProjectFiles;

public class when_comparing_snapshots : Specification
{
    string _project;
    IEnumerable<string> _changed;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), $"cratis-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_project, "src"));
        Directory.CreateDirectory(Path.Combine(_project, "node_modules"));
        File.WriteAllText(Path.Combine(_project, "kept.txt"), "kept");
        File.WriteAllText(Path.Combine(_project, "removed.txt"), "removed");
        File.WriteAllText(Path.Combine(_project, "src", "changed.txt"), "before");
    }

    void Destroy() => Directory.Delete(_project, true);

    void Because()
    {
        var before = ProjectFiles.Snapshot(_project);
        File.Delete(Path.Combine(_project, "removed.txt"));
        File.WriteAllText(Path.Combine(_project, "src", "changed.txt"), "changed after");
        File.WriteAllText(Path.Combine(_project, "src", "added.txt"), "added");
        File.WriteAllText(Path.Combine(_project, "node_modules", "package.js"), "restored");
        _changed = ProjectFiles.Changed(before, ProjectFiles.Snapshot(_project));
    }

    [Fact] void should_name_the_added_changed_and_removed_files() => _changed.ShouldContainOnly("src/added.txt", "src/changed.txt", "removed.txt");
}
