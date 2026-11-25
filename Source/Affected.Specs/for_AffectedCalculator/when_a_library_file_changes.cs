// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_library_file_changes : Specification
{
    AffectedResult _result;

    void Because()
    {
        var graph = Projects.Graph(
            Projects.Project("Source/Core/Core.csproj"),
            Projects.Project("Source/App/App.csproj", references: ["Source/Core/Core.csproj"]),
            Projects.Project("Source/App.Specs/App.Specs.csproj", test: true, references: ["Source/App/App.csproj"]),
            Projects.Project("Source/Other/Other.csproj"),
            Projects.Project("Source/Other.Specs/Other.Specs.csproj", test: true, references: ["Source/Other/Other.csproj"]));
        _result = AffectedCalculator.Calculate(graph, graph, Maps.Modified("Source/Core/Thing.cs"), new AffectedConfiguration());
    }

    [Fact] void should_not_run_everything() => _result.RunEverything.ShouldBeFalse();
    [Fact] void should_report_the_changed_project() => _result.ChangedProjects.ShouldContainOnly("Source/Core/Core.csproj");
    [Fact] void should_affect_the_transitive_dependents() => _result.AffectedProjects.ShouldContainOnly("Source/App.Specs/App.Specs.csproj", "Source/App/App.csproj", "Source/Core/Core.csproj");
    [Fact] void should_affect_only_the_reachable_test_project() => _result.TestProjects.Select(project => project.Path).ShouldContainOnly("Source/App.Specs/App.Specs.csproj");
    [Fact] void should_not_force_that_test_project_to_run_in_full() => _result.TestProjects.Single().RunAll.ShouldBeFalse();
}
