// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_generation_reports_only_warnings : given.a_generate_screenplay_command
{
    int _result;

    void Establish() =>
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(
                GeneratedSource,
                [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null)])));

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_still_write_the_document() => _standardOutput.ToArray().ShouldEqual(Encoding.UTF8.GetBytes(GeneratedSource));
}
