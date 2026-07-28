// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating.and_generation_reports_an_error.given;

/// <summary>
/// A generation that produced a document and reported an error alongside it.
/// </summary>
public class a_generation_reporting_an_error : for_GenerateScreenplayCommand.given.a_generate_screenplay_command
{
    void Establish() =>
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(
                GeneratedSource,
                [
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null),
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "an error", "Library.Lending")
                ])));
}
