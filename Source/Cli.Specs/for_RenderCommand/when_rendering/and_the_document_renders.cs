// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_document_renders : given.a_render_command
{
    int _result;

    void Establish() => _settings.Path = "MyApp.play";

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_render_the_resolved_document() =>
        _rendering.Received(1).Render(_document, Path.Combine(_folder, RenderCommand.DefaultTarget));
}
