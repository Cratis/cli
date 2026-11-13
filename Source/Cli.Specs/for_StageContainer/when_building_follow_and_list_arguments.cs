// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer;

public class when_building_follow_and_list_arguments : Specification
{
    IReadOnlyList<string> _logs;
    IReadOnlyList<string> _list;

    void Because()
    {
        _logs = StageContainer.BuildLogsArguments("cratis-stage-abc123");
        _list = StageContainer.BuildListArguments();
    }

    [Fact] void should_follow_the_named_container() => _logs.ShouldContainOnly("logs", "-f", "cratis-stage-abc123");
    [Fact] void should_list_only_sandboxes_by_prefix() => _list.ShouldContain("name=^cratis-stage-");
    [Fact] void should_list_names_only() => _list.ShouldContain("{{.Names}}");
}
