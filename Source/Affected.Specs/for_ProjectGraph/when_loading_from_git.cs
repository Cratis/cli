// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_ProjectGraph;

public class when_loading_from_git : Specification
{
    PlannerRepository _repository;
    ProjectGraph _graph;
    IReadOnlyList<string> _owners;
    IReadOnlyList<string> _nobody;
    IReadOnlySet<string> _dependents;

    void Because()
    {
        _repository = new PlannerRepository();
        _graph = ProjectGraph.Load(_repository.Repository.Repository, "HEAD", new AffectedConfiguration());
        _owners = _graph.OwnersOf("Source/Foo/A.cs");
        _nobody = _graph.OwnersOf("README.md");
        _dependents = _graph.DependentsOf(["Source/Foo/Foo.csproj"]);
    }

    [Fact] void should_find_every_project() => _graph.Projects.Count.ShouldEqual(4);
    [Fact] void should_give_a_file_to_the_project_whose_directory_holds_it() => _owners.ShouldContainOnly("Source/Foo/Foo.csproj");
    [Fact] void should_give_a_file_outside_every_project_to_nobody() => _nobody.ShouldBeEmpty();
    [Fact] void should_find_dependents() => _dependents.ShouldContainOnly("Source/Foo.Specs/Foo.Specs.csproj");

    void Destroy() => _repository.Dispose();
}
