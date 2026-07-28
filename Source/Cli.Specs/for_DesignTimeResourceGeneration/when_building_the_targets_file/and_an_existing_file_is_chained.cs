// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesignTimeResourceGeneration.when_building_the_targets_file;

public class and_an_existing_file_is_chained : given.a_temporary_folder
{
    string _chained;
    string _result;

    void Establish()
    {
        _chained = Path.Combine(_folder, "Custom.After.targets");
        File.WriteAllText(_chained, "<Project />");
    }

    void Because() => _result = DesignTimeResourceGeneration.Targets(_chained);

    [Fact] void should_keep_importing_it() => _result.ShouldContain($"<Import Project=\"{_chained}\" />");
    [Fact] void should_still_run_the_resource_target_chain() => _result.ShouldContain("DependsOnTargets=\"PrepareResources\"");
}
