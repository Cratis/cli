// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_source_generator_changes : Specification
{
    AffectedResult _result;

    void Because()
    {
        var graph = Projects.Graph(
            Projects.Project("Source/Gen/Gen.csproj", generator: true),
            Projects.Project("Source/App/App.csproj", references: ["Source/Gen/Gen.csproj"], analyzers: ["Source/Gen/Gen.csproj"]),
            Projects.Project("Source/App.Specs/App.Specs.csproj", test: true, references: ["Source/App/App.csproj"]));
        _result = AffectedCalculator.Calculate(graph, graph, Maps.Modified("Source/Gen/Generator.cs"), new AffectedConfiguration());
    }

    [Fact] void should_run_dependent_test_projects_in_full() => _result.TestProjects.Single().RunAll.ShouldBeTrue();
    [Fact] void should_say_why() => _result.TestProjects.Single().Reason.ShouldContain("generator");
}
