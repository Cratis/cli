// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedCalculator;

public class when_a_global_file_is_overridden_in_configuration : Specification
{
    AffectedResult _editorconfig;
    AffectedResult _custom;

    void Because()
    {
        var graph = Projects.LibraryWithSpecs();
        var configuration = new AffectedConfiguration
        {
            RunEverythingTriggers = ["build/shared.props"],
            MappedPaths = [".editorconfig"]
        };
        _editorconfig = AffectedCalculator.Calculate(graph, graph, Maps.Modified(".editorconfig"), configuration);
        _custom = AffectedCalculator.Calculate(graph, graph, Maps.Modified("build/shared.props"), configuration);
    }

    [Fact] void should_not_treat_a_mapped_file_as_a_trigger() => _editorconfig.RunEverything.ShouldBeFalse();
    [Fact] void should_run_everything_for_the_configured_trigger() => _custom.RunEverything.ShouldBeTrue();
}
