// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_two_libraries_remain : Specification
{
    string? _result;
    IReadOnlyList<ScreenplayProjectCandidate> _narrowed;

    void Because()
    {
        ScreenplayProjectCandidate[] candidates =
        [
            new("MyApp.Ordering", false),
            new("MyApp.Billing", false)
        ];

        _narrowed = ScreenplayProjectSelection.Narrow(candidates);
        _result = ScreenplayProjectSelection.Select(candidates);
    }

    [Fact] void should_not_select_any_of_them() => _result.ShouldBeNull();
    [Fact] void should_keep_both_as_candidates() => _narrowed.Count.ShouldEqual(2);
    [Fact] void should_order_the_candidates_by_name() => _narrowed[0].Name.ShouldEqual("MyApp.Billing");
}
