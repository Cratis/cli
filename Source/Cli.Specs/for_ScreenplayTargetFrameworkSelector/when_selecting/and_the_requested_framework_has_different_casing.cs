// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_the_requested_framework_has_different_casing : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        ["Application(net9.0-windows)", "Application(net9.0)"],
        "NET9.0");

    [Fact] void should_select_the_exact_framework_case_insensitively() => _result.ProjectNames.ShouldContainOnly(["Application(net9.0)"]);
    [Fact] void should_not_report_an_error() => _result.Diagnostics.ShouldBeEmpty();
}
