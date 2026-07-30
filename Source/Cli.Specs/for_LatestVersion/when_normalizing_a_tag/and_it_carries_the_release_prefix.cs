// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_normalizing_a_tag;

public class and_it_carries_the_release_prefix : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.NormalizeTag("v2.3.6");

    [Fact] void should_drop_the_prefix() => _result.ShouldEqual("2.3.6");
}
