// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_deciding_whether_a_project_holds_specs;

public class and_it_is_a_multi_targeted_specs_project : Specification
{
    bool _result;

    void Because() => _result = ScreenplayProjectSelection.IsSpecProject("MyApp.Specs(net10.0)");

    [Fact] void should_recognize_it_as_specs() => _result.ShouldBeTrue();
}
