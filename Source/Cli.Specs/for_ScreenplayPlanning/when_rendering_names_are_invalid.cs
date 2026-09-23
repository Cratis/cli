// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_rendering_names_are_invalid : given.a_canonical_screenplay
{
    [Theory]
    [InlineData("../escape", null)]
    [InlineData("", null)]
    [InlineData("class", null)]
    [InlineData(null, "Company..Projects")]
    [InlineData(null, "Company.class")]
    [InlineData(null, " Company.Projects")]
    public async Task should_report_the_package_validation_error_without_planning_artifacts(string? projectName, string? rootNamespace)
    {
        var source = WriteSource("single");

        var result = await _planning.Plan(new(source, Corpus.ApplicationName, CratisRendering.TargetId, projectName, rootNamespace), CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Artifacts.ShouldBeNull();
        result.Diagnostics.Select(_ => _.Code).ShouldEqual("CLI-RENDER-002");
        result.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
        _requests.ShouldBeEmpty();
    }
}
