// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_a_manifest_path_that_escapes_the_destination : given.a_rendered_destination
{
    (string? Version, Cratis.Cli.Commands.Screenplay.ScreenplayDiagnostic? Error) _result;

    void Establish() => ListedArtifact("../outside/Directory.Build.props");

    void Because() => _result = RenderProfileVersion.Resolve(_destination, upgrade: false);

    [Fact] void should_refuse_with_the_unreadable_code() => _result.Error!.Code.ShouldEqual(RenderProfileVersion.UnreadableCode);
    [Fact] void should_not_select_a_version() => _result.Version.ShouldBeNull();
}
