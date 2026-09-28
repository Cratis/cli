// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_if_newer;

/// <summary>
/// A value cached in an older form cannot be read as a comparison.
/// </summary>
public class and_the_cached_value_is_not_a_comparison : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.IsNewer("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact] void should_not_report_an_update() => _result.ShouldBeFalse();
}
