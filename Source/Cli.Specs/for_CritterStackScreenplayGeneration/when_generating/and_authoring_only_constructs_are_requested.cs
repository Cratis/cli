// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_authoring_only_constructs_are_requested : given.a_marten_application_built_from_source
{
    GeneratedScreenplay _result;

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking/Banking.csproj",
        ScreenplayGenerationOptions.Default with
        {
            Provider = ScreenplayProviders.Marten,
            AuthoringOnlyConstructs = true
        });

    [Fact] void should_report_that_the_option_was_not_applied() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnsupportedGenerationOption).Message.ShouldContain("--authoring-only-constructs");
    [Fact] void should_report_a_warning() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnsupportedGenerationOption).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_still_generate_the_document() => _result.Source.ShouldNotBeEmpty();
}
