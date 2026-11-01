// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_file_header_is_given : given.a_render_command
{
    int _result;

    void Establish() => _settings.FileHeader = ["Copyright (c) {year} Acme.", "Proprietary.\\nConfidential."];
    async Task Because() => _result = await Execute();

    [Fact] void should_return_success() => _result.ShouldEqual(ExitCodes.Success);
    [Fact]
    void should_plan_with_the_resolved_header() => _planning.Received(1).Plan(
        Arg.Is<ScreenplayRenderRequest>(request => request.FileHeader.SequenceEqual(new[] { $"Copyright (c) {DateTimeOffset.UtcNow.Year} Acme.", "Proprietary.", "Confidential." })),
        Arg.Any<CancellationToken>());
}
