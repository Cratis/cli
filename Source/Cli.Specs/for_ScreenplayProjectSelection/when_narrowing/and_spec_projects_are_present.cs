// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_spec_projects_are_present : Specification
{
    string? _result;

    void Because() => _result = ScreenplayProjectSelection.Select(
    [
        new ScreenplayProjectCandidate("MyApp", false),
        new ScreenplayProjectCandidate("MyApp.Specs", false),
        new ScreenplayProjectCandidate("MyApp.Tests", false)
    ]);

    [Fact] void should_select_the_only_project_that_is_not_specs() => _result.ShouldEqual("MyApp");
}
