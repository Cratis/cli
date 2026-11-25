// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_non_source_file_of_a_project_changes : Specification
{
    AffectedResult _embedded;
    AffectedResult _linked;

    void Because()
    {
        var graph = Projects.Graph(
            Projects.Project("Source/Foo/Foo.csproj"),
            Projects.Project("Source/Linked/Linked.csproj", externals: ["Source/Shared/"]),
            Projects.Project("Source/Foo.Specs/Foo.Specs.csproj", test: true, references: ["Source/Foo/Foo.csproj"]),
            Projects.Project("Source/Linked.Specs/Linked.Specs.csproj", test: true, references: ["Source/Linked/Linked.csproj"]));
        _embedded = AffectedCalculator.Calculate(graph, graph, Maps.Modified("Source/Foo/appsettings.json"), new AffectedConfiguration());
        _linked = AffectedCalculator.Calculate(graph, graph, Maps.Modified("Source/Shared/Common.cs"), new AffectedConfiguration());
    }

    [Fact] void should_count_as_a_change_to_the_project() => _embedded.ChangedProjects.ShouldContainOnly("Source/Foo/Foo.csproj");
    [Fact] void should_affect_the_dependent_test_project() => _embedded.TestProjects.Single().Path.ShouldEqual("Source/Foo.Specs/Foo.Specs.csproj");
    [Fact] void should_count_a_linked_file_as_a_change_to_the_project_that_links_it() => _linked.ChangedProjects.ShouldContainOnly("Source/Linked/Linked.csproj");
}
