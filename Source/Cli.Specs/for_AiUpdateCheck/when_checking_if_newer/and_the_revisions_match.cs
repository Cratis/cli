// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_checking_if_newer;

public class and_the_revisions_match : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.IsNewer("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact] void should_not_report_an_update() => _result.ShouldBeFalse();
}
