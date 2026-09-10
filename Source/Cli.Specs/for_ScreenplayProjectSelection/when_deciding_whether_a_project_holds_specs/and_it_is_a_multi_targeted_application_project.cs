// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_deciding_whether_a_project_holds_specs;

public class and_it_is_a_multi_targeted_application_project : Specification
{
    bool _result;

    void Because() => _result = ScreenplayProjectSelection.IsSpecProject("MyApp.Domain(netstandard2.0)");

    [Fact] void should_take_it_for_part_of_the_application() => _result.ShouldBeFalse();
}
