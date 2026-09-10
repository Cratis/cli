// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_several_project_groups_are_present : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        [
            "Worker(net9.0)",
            "Api(net8.0)",
            "Shared",
            "Worker(net8.0)",
            "Api(net9.0)"
        ],
        "net9.0");

    [Fact] void should_select_the_requested_variant_from_every_multi_target_group() => _result.ProjectNames.ShouldContainOnly(
        ["Api(net9.0)", "Shared", "Worker(net9.0)"]);
    [Fact] void should_preserve_project_ordering() => _result.ProjectNames.SequenceEqual(
        ["Api(net9.0)", "Shared", "Worker(net9.0)"]).ShouldBeTrue();
    [Fact] void should_not_report_an_error() => _result.Diagnostics.ShouldBeEmpty();
}
