// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_file_header_file_is_missing : given.a_render_command
{
    int _result;

    void Establish() => _settings.FileHeaderFile = "missing.txt";
    async Task Because() => _result = await Execute();

    [Fact] void should_report_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_plan() => _planning.DidNotReceive().Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>());
}
