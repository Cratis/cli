// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_an_unreadable_manifest : given.a_rendered_destination
{
    (string? Version, Cratis.Cli.Commands.Screenplay.ScreenplayDiagnostic? Error) _result;

    void Establish() => File.WriteAllText(Path.Combine(_destination, ArtifactPublicationStorage.ManifestFileName), "{ not json");

    void Because() => _result = RenderProfileVersion.Resolve(_destination, upgrade: false);

    [Fact] void should_refuse_with_the_unreadable_code() => _result.Error!.Code.ShouldEqual(RenderProfileVersion.UnreadableCode);
    [Fact] void should_not_select_a_version() => _result.Version.ShouldBeNull();
}
