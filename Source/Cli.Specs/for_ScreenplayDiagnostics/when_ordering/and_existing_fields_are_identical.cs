// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayDiagnostics.when_ordering;

public class and_existing_fields_are_identical : Specification
{
    static readonly ScreenplayDiagnostic[] _expected =
    [
        Diagnostic(null, null),
        Diagnostic(null, "Conflict"),
        Diagnostic("dotnet://Application/Accounts.Close", null),
        Diagnostic("dotnet://Application/Accounts.Close", "Conflict"),
        Diagnostic("dotnet://Application/Accounts.Open", "Unsupported")
    ];

    IReadOnlyList<ScreenplayDiagnostic> _forward;
    IReadOnlyList<ScreenplayDiagnostic> _reversed;

    void Because()
    {
        _forward = ScreenplayDiagnostics.Order(_expected);
        _reversed = ScreenplayDiagnostics.Order(_expected.Reverse());
    }

    [Fact] void should_order_by_subject_then_outcome() => _forward.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_have_the_exact_same_order_for_reversed_input() => _reversed.SequenceEqual(_expected).ShouldBeTrue();

    static ScreenplayDiagnostic Diagnostic(string? subject, string? outcome) =>
        new(ScreenplayDiagnosticSeverity.Error, "DOTNETSP0013", "conflicting placement", "Source/Application.cs")
        {
            Subject = subject,
            Outcome = outcome
        };
}
