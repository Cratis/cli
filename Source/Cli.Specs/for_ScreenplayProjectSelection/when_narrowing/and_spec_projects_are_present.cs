// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_spec_projects_are_present : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        "MyApp",
        "MyApp.Specs",
        "MyApp.Tests"
    ]);

    [Fact] void should_keep_only_the_project_that_is_not_specs() => _result.ShouldContainOnly(["MyApp"]);
}
