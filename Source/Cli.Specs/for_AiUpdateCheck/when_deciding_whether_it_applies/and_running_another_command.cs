// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_deciding_whether_it_applies;

public class and_running_another_command : Specification
{
    bool _result;

    void Because() => _result = AiUpdateCheck.AppliesTo(["chronicle", "observers", "list"]);

    [Fact] void should_apply() => _result.ShouldBeTrue();
}
