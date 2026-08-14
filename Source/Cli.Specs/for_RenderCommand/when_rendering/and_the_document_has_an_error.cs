// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_document_has_an_error : given.a_render_command
{
    int _result;

    void Establish() =>
        _rendering
            .Render(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RenderedScreenplay(
                1,
                [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "PLAY0001", "an error", "MyApp.play(3,1)")],
                []));

    async Task Because() => _result = await Execute();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
