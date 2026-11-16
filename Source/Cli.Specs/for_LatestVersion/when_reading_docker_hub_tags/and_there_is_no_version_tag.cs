// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_reading_docker_hub_tags;

public class and_there_is_no_version_tag : Specification
{
    string? _result;

    void Because() => _result = LatestVersion.HighestDockerHubTag("""{"count":2,"results":[{"name":"latest"},{"name":"3.0.0-rc.1"}]}""");

    [Fact] void should_find_none() => _result.ShouldBeNull();
}
