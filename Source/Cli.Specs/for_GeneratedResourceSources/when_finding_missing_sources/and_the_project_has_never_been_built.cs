// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedResourceSources.when_finding_missing_sources;

public class and_the_project_has_never_been_built : given.an_intermediate_output_folder
{
    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedResourceSources.MissingFrom(
        Path.Combine(_folder, "obj", "Release", "net10.0", "MyApp.dll"),
        []);

    [Fact] void should_find_nothing_missing() => _result.ShouldBeEmpty();
}
