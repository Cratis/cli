// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_TestSelector;

public class when_a_global_file_changed_since_the_map : Specification
{
    ProjectSelection _trigger;
    ProjectSelection _projectFile;

    void Because()
    {
        var map = Maps.For("base", ("Foo.Specs.A.when_a", ["Source/Foo/A.cs"]), ("Foo.Specs.B.when_b", ["Source/Foo/B.cs"]));
        _trigger = Maps.Select(map, Maps.Modified("Directory.Packages.props"));
        _projectFile = Maps.Select(map, Maps.Modified(Maps.TestProject));
    }

    [Fact] void should_run_everything_for_a_trigger() => _trigger.Mode.ShouldEqual(SelectionMode.All);
    [Fact] void should_run_everything_when_the_test_project_file_changed() => _projectFile.Mode.ShouldEqual(SelectionMode.All);
}
