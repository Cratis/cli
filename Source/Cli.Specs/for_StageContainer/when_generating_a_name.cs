// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer;

public class when_generating_a_name : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = StageContainer.GenerateName();
        _second = StageContainer.GenerateName();
    }

    [Fact] void should_recognize_the_name_as_a_stage_sandbox() => _first.StartsWith(StageContainer.NamePrefix, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_be_unique_so_sandboxes_can_run_side_by_side() => _first.ShouldNotEqual(_second);
    [Fact] void should_stay_short_enough_to_read_in_docker_ps() => _first.Length.ShouldEqual(StageContainer.NamePrefix.Length + 8);
}
