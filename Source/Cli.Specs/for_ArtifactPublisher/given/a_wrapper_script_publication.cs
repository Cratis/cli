// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ArtifactPublisher.given;

public class a_wrapper_script_publication : an_artifact_publication
{
    protected const UnixFileMode Executable = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    async Task Establish()
    {
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call => ArtifactRenderPlan.Create(
            call.Arg<ArtifactRenderRequest>(),
            [
                PlannedArtifact.CreateText("mvnw", "#!/bin/sh\n"),
                PlannedArtifact.CreateText("gradlew", "#!/bin/sh\n"),
                PlannedArtifact.CreateText("scripts/start.sh", "#!/bin/sh\n"),
                PlannedArtifact.CreateText("pom.xml", "<project/>\n")
            ],
            [],
            [],
            [],
            []));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
        _plan = (await Plan()).Artifacts!;
    }

    protected bool HasOwnerExecute(string relativePath) =>
        OperatingSystem.IsWindows() || File.GetUnixFileMode(ArtifactPath(relativePath)).HasFlag(UnixFileMode.UserExecute);

    protected bool HasAnyExecute(string relativePath) =>
        !OperatingSystem.IsWindows() && (File.GetUnixFileMode(ArtifactPath(relativePath)) & Executable) != UnixFileMode.None;

    protected void RemoveExecute(string relativePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            var path = ArtifactPath(relativePath);
            File.SetUnixFileMode(path, File.GetUnixFileMode(path) & ~Executable);
        }
    }
}
