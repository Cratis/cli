// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_mapping_a_diagnostic;

public class with_typed_placement_fields : Specification
{
    ScreenplayDiagnostic _result;

    void Because() => _result = CritterStackScreenplayGeneration.Map(new GenerationDiagnostic
    {
        Code = "DOTNETSP0013",
        Severity = GenerationDiagnosticSeverity.Error,
        Message = "conflicting source owners",
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Subject = new SubjectId { Value = "dotnet://Application/Orders.PlaceOrder" }
    });

    [Fact] void should_preserve_the_subject() => _result.Subject.ShouldEqual("dotnet://Application/Orders.PlaceOrder");
    [Fact] void should_preserve_the_outcome() => _result.Outcome.ShouldEqual("Conflict");
    [Fact] void should_preserve_the_code() => _result.Code.ShouldEqual("DOTNETSP0013");
}
