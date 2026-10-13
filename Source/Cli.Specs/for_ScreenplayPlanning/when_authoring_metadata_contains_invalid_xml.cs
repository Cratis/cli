// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_authoring_metadata_contains_invalid_xml : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllText(_file, Source.Replace(
        "      command RegisterProject\n",
        "      command RegisterProject\n        description \"Invalid \u0001 XML\"\n",
        StringComparison.Ordinal));

    async Task Because() => _result = await Plan();

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_plan_any_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_a_blocking_render_contract_diagnostic() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "CLI-RENDER-005").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_preserve_the_renderer_error_message() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "CLI-RENDER-005").Message.ShouldContain("invalid XML characters");
}
