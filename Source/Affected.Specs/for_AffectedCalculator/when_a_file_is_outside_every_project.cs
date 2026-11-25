// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_file_is_outside_every_project : Specification
{
    AffectedResult _unmapped;
    AffectedResult _mapped;
    AffectedResult _map;

    void Because()
    {
        var graph = Projects.LibraryWithSpecs();
        _unmapped = AffectedCalculator.Calculate(graph, graph, Maps.Modified("scripts/deploy.sh"), new AffectedConfiguration());
        _mapped = AffectedCalculator.Calculate(graph, graph, Maps.Modified("scripts/deploy.sh"), new AffectedConfiguration { MappedPaths = ["scripts/**"] });
        _map = AffectedCalculator.Calculate(graph, graph, Maps.Modified(".cratis/affected/maps/Source/Foo.Specs/Foo.Specs.csproj.json"), new AffectedConfiguration());
    }

    [Fact] void should_run_everything_when_it_is_not_mapped() => _unmapped.RunEverything.ShouldBeTrue();
    [Fact] void should_name_the_file() => _unmapped.RunEverythingReasons.Single().ShouldContain("scripts/deploy.sh");
    [Fact] void should_not_run_everything_when_it_is_mapped_to_another_job() => _mapped.RunEverything.ShouldBeFalse();
    [Fact] void should_ignore_coverage_maps() => _map.RunEverything.ShouldBeFalse();
    [Fact] void should_affect_nothing_for_a_coverage_map_change() => _map.AffectedProjects.ShouldBeEmpty();
}
