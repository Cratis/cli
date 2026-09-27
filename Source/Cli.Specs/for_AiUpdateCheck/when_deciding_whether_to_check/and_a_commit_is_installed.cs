// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_deciding_whether_to_check;

public class and_a_commit_is_installed : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.ShouldCheck("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null);

    [Fact] void should_check() => _result.ShouldBeTrue();
}
