// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GeneratedResourceSources.when_finding_missing_sources;

public class and_the_intermediate_assembly_is_not_known : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = GeneratedResourceSources.MissingFrom(null, []);

    [Fact] void should_find_nothing_missing() => _result.ShouldBeEmpty();
}
