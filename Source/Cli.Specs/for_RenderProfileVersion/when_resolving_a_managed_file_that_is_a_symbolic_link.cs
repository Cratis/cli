// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_a_managed_file_that_is_a_symbolic_link : given.a_rendered_destination
{
    (string? Version, Cratis.Cli.Commands.Screenplay.ScreenplayDiagnostic? Error) _result;
    string _target = null!;

    void Establish()
    {
        _target = Path.Combine(Path.GetTempPath(), $"cli-profile-link-target-{Guid.NewGuid():N}.props");
        File.WriteAllText(_target, "<Project><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        File.CreateSymbolicLink(Path.Combine(_destination, "Directory.Build.props"), _target);
        ListedArtifact("Directory.Build.props");
    }

    void Because() => _result = RenderProfileVersion.Resolve(_destination, upgrade: false);

    void Destroy() => File.Delete(_target);

    [Fact] void should_refuse_with_the_unreadable_code() => _result.Error!.Code.ShouldEqual(RenderProfileVersion.UnreadableCode);
    [Fact] void should_not_select_a_version() => _result.Version.ShouldBeNull();
}
