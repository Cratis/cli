// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_removing_the_target_framework;

public class and_the_project_targets_one_framework : Specification
{
    string _result;

    void Because() => _result = ScreenplayProjectSelection.WithoutTargetFramework("MyApp");

    [Fact] void should_leave_the_name_untouched() => _result.ShouldEqual("MyApp");
}
