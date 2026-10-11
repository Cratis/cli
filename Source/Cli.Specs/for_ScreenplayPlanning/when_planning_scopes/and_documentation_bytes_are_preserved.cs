// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_documentation_bytes_are_preserved : given.a_scoped_model
{
    ScreenplayRenderPlan _full = null!;
    ScreenplayRenderPlan _scoped = null!;

    async Task Establish() => _full = await Plan();
    async Task Because() => _scoped = await Plan("Projects.Registration.RegisterProject", "Tasks.Backlog");

    [Fact] void should_plan_both_scopes() => _scoped.Success.ShouldBeTrue();
    [Fact] void should_carry_additional_scopes() => _scoped.Artifacts!.AdditionalScopes.Length.ShouldEqual(1);
    [Fact] void should_preserve_every_artifact_byte() => _scoped.Artifacts!.Artifacts.All(artifact => artifact.Bytes.SequenceEqual(_full.Artifacts!.Artifacts.Single(expected => expected.RelativePath == artifact.RelativePath).Bytes)).ShouldBeTrue();
    [Fact] void should_include_authored_documentation() => Encoding.UTF8.GetString(_scoped.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("Registers &lt;project&gt; &amp; name");
    [Fact] void should_exclude_the_scaffold_and_scene() => _scoped.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath is "Program.cs" or "scene.json").ShouldBeFalse();
}
