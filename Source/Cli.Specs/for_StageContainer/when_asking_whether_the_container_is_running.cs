// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer;

/// <summary>
/// Whether the sandbox is still up is a question only Docker can answer.
/// </summary>
/// <remarks>
/// The Docker client a run starts is a different process from the container it asked for, and it can exit
/// while the container keeps running. Treating the client's exit as the container's is how a run reported a
/// clean stop and left a container up, holding the port the next run wanted.
/// </remarks>
public class when_asking_whether_the_container_is_running : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildIsRunningArguments("cratis-stage-abc123");

    [Fact] void should_ask_docker_for_running_containers() => _arguments.ShouldContain("ps");
    [Fact] void should_ask_for_ids_only() => _arguments.ShouldContain("--quiet");

    /// <summary>
    /// Anchored, so a container whose name merely contains this one cannot answer for it - several sandboxes
    /// running side by side is the ordinary case, not the unusual one.
    /// </summary>
    [Fact] void should_match_the_whole_name() => _arguments.ShouldContain("name=^cratis-stage-abc123$");

    [Fact] void should_read_an_id_as_still_running() => StageContainer.IsRunningFrom("8276952f0a1b\n").ShouldBeTrue();
    [Fact] void should_read_nothing_as_gone() => StageContainer.IsRunningFrom(string.Empty).ShouldBeFalse();
    [Fact] void should_read_blank_output_as_gone() => StageContainer.IsRunningFrom("\n  \n").ShouldBeFalse();
}
