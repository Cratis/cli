// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_provenance_is_available : given.a_generate_screenplay_command
{
    TextWriter _previousError;
    StringWriter _capturedError;
    int _result;

    void Establish()
    {
        _previousError = Console.Error;
        _capturedError = new StringWriter();
        Console.SetError(_capturedError);
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(GeneratedSource, [])
            {
                Provenance = new ScreenplayGenerationProvenance(
                    ScreenplayProviders.Marten,
                    "0.3.0",
                    [],
                    new ScreenplayCompatibilityReport(
                        ScreenplaySupportTier.Canonical,
                        ScreenplayRecognitionStatus.Recognized,
                        ScreenplaySemanticConformance.RequiresHumanReview,
                        ScreenplayLoweringFidelity.NoReportedLoss,
                        "canonical fixture evidence"))
            }));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_compatibility_to_standard_error() => _capturedError.ToString().ShouldContain("\"supportTier\":\"Canonical\"");
    [Fact] void should_keep_the_screenplay_clean_on_standard_output() => Encoding.UTF8.GetString(_standardOutput.ToArray()).ShouldEqual(GeneratedSource);

    void Destroy()
    {
        Console.SetError(_previousError);
        _capturedError.Dispose();
    }
}
