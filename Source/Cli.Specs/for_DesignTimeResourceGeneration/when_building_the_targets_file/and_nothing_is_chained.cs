// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_building_the_targets_file;

public class and_nothing_is_chained : Specification
{
    string _result;

    void Because() => _result = DesignTimeResourceGeneration.Targets(null);

    [Fact] void should_run_the_resource_target_chain() => _result.ShouldContain("DependsOnTargets=\"PrepareResources\"");
    [Fact] void should_run_it_before_the_compiler_inputs_are_gathered() => _result.ShouldContain("BeforeTargets=\"CoreCompile\"");
    [Fact] void should_not_import_anything() => _result.ShouldNotContain("<Import");
}
