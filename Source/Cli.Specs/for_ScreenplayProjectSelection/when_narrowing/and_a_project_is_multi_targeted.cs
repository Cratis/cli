// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_a_project_is_multi_targeted : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        "MyApp(net10.0)",
        "MyApp(netstandard2.0)"
    ]);

    [Fact] void should_take_the_target_frameworks_for_one_project() => _result.ShouldContainOnly(["MyApp"]);
}
