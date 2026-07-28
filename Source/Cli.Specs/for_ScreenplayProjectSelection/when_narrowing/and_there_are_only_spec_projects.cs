// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_there_are_only_spec_projects : Specification
{
    IReadOnlyList<ScreenplayProjectCandidate> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        new ScreenplayProjectCandidate("MyApp.Specs", false),
        new ScreenplayProjectCandidate("MyApp.IntegrationTests", true)
    ]);

    [Fact] void should_leave_nothing_to_generate_from() => _result.ShouldBeEmpty();
}
