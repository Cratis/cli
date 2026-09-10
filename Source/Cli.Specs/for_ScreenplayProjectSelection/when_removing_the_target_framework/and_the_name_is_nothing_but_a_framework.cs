// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_removing_the_target_framework;

public class and_the_name_is_nothing_but_a_framework : Specification
{
    string _result;

    void Because() => _result = ScreenplayProjectSelection.WithoutTargetFramework("(net10.0)");

    [Fact] void should_leave_the_name_untouched_rather_than_leave_nothing() => _result.ShouldEqual("(net10.0)");
}
