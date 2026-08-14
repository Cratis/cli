// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_no_documents_are_found : given.a_render_command
{
    int _result;

    void Establish() => _rendering.Render(Arg.Any<string>(), Arg.Any<string>()).Returns(new RenderedScreenplay(0, [], []));

    async Task Because() => _result = await Execute();

    [Fact] void should_report_that_nothing_was_found() => _result.ShouldEqual(ExitCodes.NotFound);
}
