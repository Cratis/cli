// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_repository_root_workspace : given.a_workspace_render_command
{
    int _result;

    void Establish()
    {
        WriteWorkspace(CreateWorkspace());
        _settings.RepositoryRoot = true;
        _command = new RenderCommand(new ScreenplayPlanning(), new ArtifactPublisher());
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_emit_repository_ci() => File.Exists(Path.Combine(_destination, ".github/workflows/build.yml")).ShouldBeTrue();
}
