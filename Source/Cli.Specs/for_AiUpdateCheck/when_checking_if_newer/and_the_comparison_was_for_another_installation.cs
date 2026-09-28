// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_if_newer;

/// <summary>
/// Once 'cratis ai update' has run, a comparison made for the previous commit says nothing about the new one.
/// </summary>
public class and_the_comparison_was_for_another_installation : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.IsNewer("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:12", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact] void should_not_report_an_update() => _result.ShouldBeFalse();
}
