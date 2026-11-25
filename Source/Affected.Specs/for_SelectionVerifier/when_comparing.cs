// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_SelectionVerifier;

public class when_comparing : Specification
{
    string? _equal;
    string? _missing;
    string? _extra;

    void Because()
    {
        HashSet<string> expected = ["A", "B"];
        static HashSet<string> Set(params string[] values) => [.. values];
        _equal = SelectionVerifier.Compare(expected, Set("B", "A"));
        _missing = SelectionVerifier.Compare(expected, Set("A"));
        _extra = SelectionVerifier.Compare(expected, Set("A", "B", "A2"));
    }

    [Fact] void should_accept_equal_sets() => _equal.ShouldBeNull();
    [Fact] void should_report_a_selected_unit_that_did_not_match() => _missing.ShouldContain("1 selected unit(s) not matched");
    [Fact] void should_report_an_unselected_unit_that_matched() => _extra.ShouldContain("A2");
}
