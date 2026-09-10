// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_a_single_target_project_is_also_present : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        ["Application(net8.0)", "Application(net9.0)", "Shared"],
        "net9.0");

    [Fact] void should_keep_the_selected_application_framework() => _result.ProjectNames.ShouldContain("Application(net9.0)");
    [Fact] void should_keep_the_single_target_project() => _result.ProjectNames.ShouldContain("Shared");
    [Fact] void should_not_report_an_error() => _result.Diagnostics.ShouldBeEmpty();
}
