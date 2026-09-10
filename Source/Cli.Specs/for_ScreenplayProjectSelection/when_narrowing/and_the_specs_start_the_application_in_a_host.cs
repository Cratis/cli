// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_the_specs_start_the_application_in_a_host : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        "MyApp",
        "MyApp.Specs",
        "MyApp.Specs.AppHost"
    ]);

    [Fact] void should_leave_the_host_out_along_with_the_specs() => _result.ShouldContainOnly(["MyApp"]);
}
