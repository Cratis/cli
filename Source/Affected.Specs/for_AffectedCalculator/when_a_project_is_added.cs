// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_project_is_added : Specification
{
    AffectedResult _result;

    void Because()
    {
        var baseGraph = Projects.LibraryWithSpecs();
        var headGraph = Projects.Graph(
            Projects.Project("Source/Foo/Foo.csproj"),
            Projects.Project("Source/Foo.Specs/Foo.Specs.csproj", test: true, references: ["Source/Foo/Foo.csproj"]),
            Projects.Project("Source/Bar/Bar.csproj", references: ["Source/Foo/Foo.csproj"]),
            Projects.Project("Source/Bar.Specs/Bar.Specs.csproj", test: true, references: ["Source/Bar/Bar.csproj"]));
        _result = AffectedCalculator.Calculate(
            baseGraph,
            headGraph,
            [new FileChange(ChangeKind.Added, "Source/Bar/Bar.csproj"), new FileChange(ChangeKind.Added, "Source/Bar.Specs/Bar.Specs.csproj")],
            new AffectedConfiguration());
    }

    [Fact] void should_report_the_added_projects() => _result.AddedProjects.ShouldContainOnly("Source/Bar.Specs/Bar.Specs.csproj", "Source/Bar/Bar.csproj");
    [Fact] void should_affect_the_new_projects() => _result.AffectedProjects.ShouldContainOnly("Source/Bar.Specs/Bar.Specs.csproj", "Source/Bar/Bar.csproj");
    [Fact] void should_run_the_new_test_project_in_full() => _result.TestProjects.Single().RunAll.ShouldBeTrue();
    [Fact] void should_say_it_is_new() => _result.TestProjects.Single().Reason.ShouldContain("new test project");
}
