// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_the_specs_project_is_named_without_a_prefix : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        "Core",
        "Specs",
        "Tests"
    ]);

    [Fact] void should_keep_only_the_project_that_is_not_specs() => _result.ShouldContainOnly(["Core"]);
}
