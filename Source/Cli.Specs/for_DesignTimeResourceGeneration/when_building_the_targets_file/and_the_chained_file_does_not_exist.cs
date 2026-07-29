// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_building_the_targets_file;

public class and_the_chained_file_does_not_exist : given.a_temporary_folder
{
    string _result;

    void Because() => _result = DesignTimeResourceGeneration.Targets(Path.Combine(_folder, "Missing.targets"));

    [Fact] void should_not_import_it() => _result.ShouldNotContain("<Import");
    [Fact] void should_still_run_the_resource_target_chain() => _result.ShouldContain("DependsOnTargets=\"PrepareResources\"");
}
