// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedResourceSources.when_finding_missing_sources;

public class and_none_of_them_are_compiled : given.an_intermediate_output_folder
{
    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedResourceSources.MissingFrom(_assemblyPath, []);

    [Fact] void should_find_every_generated_resource_source() => _result.Count.ShouldEqual(2);
    [Fact] void should_find_them_in_a_stable_order() => _result[0].ShouldEqual(_adminMessages);
    [Fact] void should_find_the_second_one_too() => _result[1].ShouldEqual(_commonMessages);
    [Fact] void should_leave_other_generated_sources_alone() => _result.ShouldNotContain(Path.Combine(_intermediate, "MyApp.AssemblyInfo.cs"));
}
