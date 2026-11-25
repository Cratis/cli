// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_project_is_removed : Specification
{
    AffectedResult _result;

    void Because()
    {
        var baseGraph = Projects.Graph(
            Projects.Project("Source/Old/Old.csproj"),
            Projects.Project("Source/Consumer/Consumer.csproj", references: ["Source/Old/Old.csproj"]),
            Projects.Project("Source/Consumer.Specs/Consumer.Specs.csproj", test: true, references: ["Source/Consumer/Consumer.csproj"]),
            Projects.Project("Source/Old.Specs/Old.Specs.csproj", test: true, references: ["Source/Old/Old.csproj"]),
            Projects.Project("Source/Unrelated.Specs/Unrelated.Specs.csproj", test: true));
        var headGraph = Projects.Graph(
            Projects.Project("Source/Consumer/Consumer.csproj"),
            Projects.Project("Source/Consumer.Specs/Consumer.Specs.csproj", test: true, references: ["Source/Consumer/Consumer.csproj"]),
            Projects.Project("Source/Unrelated.Specs/Unrelated.Specs.csproj", test: true));
        _result = AffectedCalculator.Calculate(
            baseGraph,
            headGraph,
            [
                new FileChange(ChangeKind.Deleted, "Source/Old/Old.csproj"),
                new FileChange(ChangeKind.Deleted, "Source/Old/Thing.cs"),
                new FileChange(ChangeKind.Deleted, "Source/Old.Specs/Old.Specs.csproj")
            ],
            new AffectedConfiguration());
    }

    [Fact] void should_report_the_removed_projects() => _result.RemovedProjects.ShouldContainOnly("Source/Old.Specs/Old.Specs.csproj", "Source/Old/Old.csproj");
    [Fact] void should_affect_what_depended_on_it_in_the_base_graph() => _result.TestProjects.Select(project => project.Path).ShouldContain("Source/Consumer.Specs/Consumer.Specs.csproj");
    [Fact] void should_not_affect_unrelated_test_projects() => _result.TestProjects.Select(project => project.Path).ShouldNotContain("Source/Unrelated.Specs/Unrelated.Specs.csproj");
    [Fact] void should_not_list_a_removed_project_as_affected() => _result.AffectedProjects.ShouldNotContain("Source/Old/Old.csproj");
}
