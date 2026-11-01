// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_file_header_file_is_given : given.a_render_command
{
    int _result;

    async Task Establish()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "header.txt"), "Copyright (c) Acme.\n");
        _settings.FileHeaderFile = "header.txt";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_return_success() => _result.ShouldEqual(ExitCodes.Success);
    [Fact]
    void should_plan_with_the_lines_of_the_file() => _planning.Received(1).Plan(
        Arg.Is<ScreenplayRenderRequest>(request => request.FileHeader.SequenceEqual(new[] { "Copyright (c) Acme." })),
        Arg.Any<CancellationToken>());
}
