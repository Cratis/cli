// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetFrameworkSelector.when_selecting;

public class and_frameworks_are_out_of_order : Specification
{
    ScreenplayTargetFrameworkSelection _result;

    void Because() => _result = ScreenplayTargetFrameworkSelector.Select(
        ["Application(net9.0)", "Application(net8.0)", "Application(net10.0)"],
        requestedFramework: null);

    [Fact] void should_order_the_available_frameworks() => _result.Diagnostics.Single().Message.ShouldEqual(
        "Project 'Application' targets multiple frameworks: net10.0, net8.0, net9.0. Pass --framework <TFM> to select one");
}
