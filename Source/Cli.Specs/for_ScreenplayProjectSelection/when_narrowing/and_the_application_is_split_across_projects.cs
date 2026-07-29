// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_narrowing;

public class and_the_application_is_split_across_projects : Specification
{
    IReadOnlyList<string> _result;

    void Because() => _result = ScreenplayProjectSelection.Narrow(
    [
        "MyApp.Domain",
        "MyApp.Api",
        "MyApp.Read"
    ]);

    [Fact] void should_keep_every_project() => _result.Count.ShouldEqual(3);
    [Fact] void should_order_the_projects_by_name() => _result[0].ShouldEqual("MyApp.Api");
    [Fact] void should_keep_the_libraries_alongside_the_executable() => _result[1].ShouldEqual("MyApp.Domain");
    [Fact] void should_keep_the_last_project_too() => _result[2].ShouldEqual("MyApp.Read");
}
