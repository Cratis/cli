// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_both_file_header_options_are_given : given.a_render_command
{
    int _result;

    void Establish()
    {
        _settings.FileHeader = ["Acme"];
        _settings.FileHeaderFile = "header.txt";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_plan() => _planning.DidNotReceive().Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>());
}
