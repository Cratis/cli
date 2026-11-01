// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_no_file_header_is_given : given.a_render_command
{
    async Task Because() => await Execute();

    [Fact] void should_plan_without_a_header() => _planning.Received(1).Plan(Arg.Is<ScreenplayRenderRequest>(request => request.FileHeader.IsEmpty), Arg.Any<CancellationToken>());
}
