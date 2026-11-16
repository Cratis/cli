// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_reading_docker_hub_tags;

/// <summary>
/// Docker Hub lists tags newest-pushed first, with the moving latest tag among them - neither the order nor latest
/// says which version is the newest.
/// </summary>
public class and_the_tags_are_mixed : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestDockerHubTag(
        """{"count":6,"results":[{"name":"latest"},{"name":"1.9.0"},{"name":"1.10.0"},{"name":"1.11.0-beta.1"},{"name":"2.0"},{"name":"1.10.0.1"}]}""");

    [Fact] void should_pick_the_highest_plain_version() => _result.ShouldEqual("1.10.0");
}
