// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_the_requested_framework_is_unavailable : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        ["Application(net8.0)", "Application(net9.0)"],
        "net10.0");

    [Fact] void should_fail_closed() => _result.IsSuccessful.ShouldBeFalse();
    [Fact] void should_report_the_unavailable_framework_code() => _result.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.UnavailableTargetFramework);
    [Fact] void should_name_the_requested_and_available_frameworks() => _result.Diagnostics.Single().Message.ShouldEqual(
        "Project 'Application' does not target requested framework 'net10.0'. Available target frameworks: net8.0, net9.0");
}
