// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_repository_root_render : a_check_command
{
    protected const string Workflow = ".github/workflows/build.yml";
    protected (int ExitCode, string Stdout, string Stderr) _output;
    protected string _workflow = null!;

    void Establish()
    {
        File.WriteAllText(_document, "module Library\n");
        _workflow = Path.Combine(_destination, Workflow);
        _command = new RenderCommand(new ScreenplayPlanning(), new ArtifactPublisher());
        _settings.Check = false;
        _settings.RepositoryRoot = true;
    }
}
