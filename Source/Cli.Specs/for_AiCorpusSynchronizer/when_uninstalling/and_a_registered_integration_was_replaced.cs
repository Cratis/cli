// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_a_registered_integration_was_replaced : given.an_ownership_plan
{
    string _path = null!;

    void Establish()
    {
        _path = Path.Combine(_project, ".pi", "skills");
        DeleteLink(_path);
        Directory.CreateDirectory(_path);
        File.WriteAllText(Path.Combine(_path, "user.txt"), "keep");
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project, force: true);

    [Fact] void should_report_the_unowned_path() => _result.Conflicts.ShouldContain(".pi/skills");
    [Fact] void should_not_remove_the_user_directory() => File.ReadAllText(Path.Combine(_path, "user.txt")).ShouldEqual("keep");
    [Fact] void should_not_remove_other_managed_files() => File.Exists(Installed("general.md")).ShouldBeTrue();
}
