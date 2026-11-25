// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_global_file_changes : Specification
{
    static readonly string[] _globals =
    [
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "global.json",
        "NuGet.config",
        ".editorconfig",
        ".globalconfig",
        ".cratis/affected.json",
        "Source/Foo/Directory.Build.props"
    ];

    List<AffectedResult> _results;

    void Because()
    {
        var graph = Projects.LibraryWithSpecs();
        _results = [.. _globals.Select(path => AffectedCalculator.Calculate(graph, graph, Maps.Modified(path), new AffectedConfiguration()))];
    }

    [Fact] void should_run_everything_for_each_of_them() => _results.ShouldEachConformTo(result => result.RunEverything);
    [Fact] void should_run_every_test_project_in_full() => _results.ShouldEachConformTo(result => result.TestProjects.All(project => project.RunAll));
    [Fact] void should_say_which_file_forced_it() => _results[1].RunEverythingReasons.Single().ShouldContain("Directory.Build.targets");
}
