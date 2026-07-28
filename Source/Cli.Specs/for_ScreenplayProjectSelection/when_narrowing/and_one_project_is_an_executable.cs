// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_one_project_is_an_executable : Specification
{
    string? _result;

    void Because() => _result = ScreenplayProjectSelection.Select(
    [
        new ScreenplayProjectCandidate("MyApp.Domain", false),
        new ScreenplayProjectCandidate("MyApp.Api", true),
        new ScreenplayProjectCandidate("MyApp.Read", false)
    ]);

    [Fact] void should_select_the_executable() => _result.ShouldEqual("MyApp.Api");
}
