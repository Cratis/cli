// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_project_is_renamed : Specification
{
    AffectedResult _result;

    void Because()
    {
        var baseGraph = Projects.Graph(
            Projects.Project("Source/Foo/Foo.csproj"),
            Projects.Project("Source/Foo.Specs/Foo.Specs.csproj", test: true, references: ["Source/Foo/Foo.csproj"]));
        var headGraph = Projects.Graph(
            Projects.Project("Source/Renamed/Renamed.csproj"),
            Projects.Project("Source/Foo.Specs/Foo.Specs.csproj", test: true, references: ["Source/Renamed/Renamed.csproj"]));
        _result = AffectedCalculator.Calculate(
            baseGraph,
            headGraph,
            [
                new FileChange(ChangeKind.Renamed, "Source/Renamed/Renamed.csproj", "Source/Foo/Foo.csproj"),
                new FileChange(ChangeKind.Renamed, "Source/Renamed/Thing.cs", "Source/Foo/Thing.cs"),
                new FileChange(ChangeKind.Modified, "Source/Foo.Specs/Foo.Specs.csproj")
            ],
            new AffectedConfiguration());
    }

    [Fact] void should_treat_the_old_project_as_removed() => _result.RemovedProjects.ShouldContainOnly("Source/Foo/Foo.csproj");
    [Fact] void should_treat_the_new_project_as_added() => _result.AddedProjects.ShouldContainOnly("Source/Renamed/Renamed.csproj");
    [Fact] void should_affect_the_dependents_of_the_renamed_project() => _result.TestProjects.Select(project => project.Path).ShouldContain("Source/Foo.Specs/Foo.Specs.csproj");
}
