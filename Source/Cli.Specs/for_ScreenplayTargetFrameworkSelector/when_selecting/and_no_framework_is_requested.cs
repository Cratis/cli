// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_no_framework_is_requested : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        ["Application(net8.0)", "Shared", "Application(net9.0)"],
        requestedFramework: null,
        "/workspace/Application.slnx");

    [Fact] void should_fail_closed() => _result.IsSuccessful.ShouldBeFalse();
    [Fact] void should_retain_the_single_target_project() => _result.ProjectNames.ShouldContainOnly(["Shared"]);
    [Fact] void should_report_the_ambiguous_framework_code() => _result.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousTargetFramework);
    [Fact] void should_name_the_project_and_available_frameworks() => _result.Diagnostics.Single().Message.ShouldContain("'Application' targets multiple frameworks: net8.0, net9.0");
    [Fact] void should_explain_how_to_select_one() => _result.Diagnostics.Single().Message.ShouldContain("--framework <TFM>");
    [Fact] void should_locate_the_diagnostic_at_the_target() => _result.Diagnostics.Single().Location.ShouldEqual("/workspace/Application.slnx");
}
