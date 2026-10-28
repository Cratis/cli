// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.for_ArtifactPublisher.given;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_package_acceptance_command : a_check_command
{
    protected bool _unsafeGeneration;

    void Establish()
    {
        File.WriteAllText(_document, "module Library\n");
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call => AcceptancePlans.Degraded(call.Arg<ArtifactRenderRequest>(), _unsafeGeneration));
        _planning = new ScreenplayPlanning(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
        _publication = new ArtifactPublisher();
        _command = new RenderCommand(_planning, _publication);
    }

    private protected Task<ScreenplayRenderPlan> PlanPackage() => _planning.Plan(
        new(_document, "MyApp", "cratis") { Strict = _settings.Strict }, CancellationToken.None);

    protected static string[] Snapshot(string destination) => !Directory.Exists(destination)
        ? ["absent"]
        : [.. Directory.GetFiles(destination, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal).Select(path => $"{Path.GetRelativePath(destination, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}")];
}
