// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_normalizing_a_tag;

public class and_it_is_empty : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.NormalizeTag("   ");

    [Fact] void should_have_nothing_to_report() => _result.ShouldBeNull();
}
