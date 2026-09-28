// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_if_newer;

public class and_the_comparison_found_new_commits : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.IsNewer("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:12", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact] void should_report_an_update() => _result.ShouldBeTrue();
}
