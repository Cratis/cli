// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedResourceSources.when_finding_missing_sources;

public class and_one_of_them_is_compiled : given.an_intermediate_output_folder
{
    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedResourceSources.MissingFrom(_assemblyPath, [_adminMessages, null]);

    [Fact] void should_find_only_the_other_one() => _result.ShouldContainOnly([_commonMessages]);
}
