// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Axon;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_using_the_bundled_renderer : for_ScreenplayPlanning.given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon", "projects-api", "com.example.projects") { FileHeader = ["Example header"] }, CancellationToken.None);

    [Fact] void should_render_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_identify_the_target() => _result.Target.Name.ShouldEqual("axon");
    [Fact] void should_use_the_facade_admission_ceiling() => new AxonRenderTarget().NewestAdmittedSemanticVersion.ShouldEqual(AxonRendering.NewestAdmittedSemanticVersion);
    [Fact] void should_plan_java() => _result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith(".java", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_map_the_project_name() => Text("pom.xml").ShouldContain("<artifactId>projects-api</artifactId>");
    [Fact] void should_map_the_java_package() => Text("pom.xml").ShouldContain("<groupId>com.example.projects</groupId>");
    [Fact] void should_map_the_header() => _result.Artifacts!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".java", StringComparison.Ordinal)).All(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).StartsWith("// Example header\n\n", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_report_an_unavailable_renderer() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "CLI-RENDER-028").ShouldBeFalse();
    [Fact] void should_emit_one_experimental_warning() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "CLI-RENDER-029").ShouldEqual(1);
    [Fact] void should_not_plan_a_cratis_scaffold() => _result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith(".csproj", StringComparison.Ordinal) || artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).ShouldBeFalse();

    string Text(string path) => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
