// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_deciding_whether_it_applies;

public class and_running_ai_uninstall : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.AppliesTo(["ai", "uninstall"]);

    [Fact] void should_not_apply() => _result.ShouldBeFalse();
}
