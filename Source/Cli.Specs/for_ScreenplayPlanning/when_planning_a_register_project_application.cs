// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_register_project_application : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await Plan();

    [Fact] void should_be_successful() => _result.Success.ShouldBeTrue();
    [Fact] void should_compile_one_document() => _result.Documents.ShouldEqual(1);
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_use_the_versioned_artifact_schema() => _result.Artifacts!.SchemaVersion.ShouldEqual("1");
    [Fact] void should_bind_the_application_name_independently() => _result.Artifacts!.ApplicationName.ShouldEqual("Projects");
    [Fact] void should_include_the_buildable_project_scaffold() => Content("Projects.csproj").ShouldContain("Cratis.Arc.Chronicle.Testing");
    [Fact] void should_include_the_generated_command() => Content("Projects/Registration/RegisterProject/RegisterProject.cs").ShouldContain("public record RegisterProject");
    [Fact] void should_include_generated_specifications() => _result.Artifacts!.Artifacts.Count(_ => _.RelativePath.Contains("when_", StringComparison.Ordinal)).ShouldEqual(4);

    string Content(string path)
    {
        var artifact = _result.Artifacts!.Artifacts.Single(_ => _.RelativePath == path);
        return Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
    }
}
