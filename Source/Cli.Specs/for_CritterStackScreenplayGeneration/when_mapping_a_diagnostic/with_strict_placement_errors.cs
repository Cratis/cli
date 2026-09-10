// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_mapping_a_diagnostic;

public class with_strict_placement_errors : Specification
{
    static readonly string[] _codes =
    [
        "DOTNETSP0002",
        "DOTNETSP0003",
        "DOTNETSP0005",
        "DOTNETSP0009",
        "DOTNETSP0010",
        "DOTNETSP0011",
        "DOTNETSP0013"
    ];

    IReadOnlyList<ScreenplayDiagnostic> _result;

    void Because() => _result =
    [
        .. _codes.Select(code => CritterStackScreenplayGeneration.Map(new GenerationDiagnostic
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Error,
            Message = $"strict placement {code}",
            Outcome = GenerationDiagnosticOutcome.Conflict,
            Subject = new SubjectId { Value = $"dotnet://Application/{code}" }
        }))
    ];

    [Fact] void should_preserve_every_strict_error_code_without_substituting_flat_compatibility() => _result.Select(_ => _.Code).ShouldEqual(_codes);
    [Fact] void should_preserve_every_subject() => _result.Select(_ => _.Subject).ShouldEqual(_codes.Select(code => $"dotnet://Application/{code}"));
    [Fact] void should_preserve_every_typed_outcome() => _result.All(_ => _.Outcome == "Conflict").ShouldBeTrue();
}
