// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer;

public class when_building_stop_arguments : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildStopArguments("cratis-stage-abc123");

    [Fact] void should_stop_only_the_named_container() => _arguments.SequenceEqual(["stop", "cratis-stage-abc123"]).ShouldBeTrue();
}
