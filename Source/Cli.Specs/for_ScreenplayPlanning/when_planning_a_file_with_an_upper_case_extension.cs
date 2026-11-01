// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_file_with_an_upper_case_extension : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    void Establish()
    {
        _path = Path.Combine(_folder, "Projects.PLAY");
        File.WriteAllText(_path, Source);
    }

    async Task Because() => _result = await Plan(_path);

    [Fact] void should_still_render() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_artifacts() => _result.Artifacts!.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_warn_that_documentation_was_left_out() => _result.Diagnostics.Single(_ => _.Code == CratisRenderTarget.AuthoringMetadataSkippedCode).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
}
