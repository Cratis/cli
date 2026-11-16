// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UpdateChecker.when_keying_the_cache;

public class and_the_source_is_docker_hub : Specification
{
    string _result = null!;

    void Because() => _result = UpdateChecker.CacheKeyFor(LatestVersionSource.DockerHub, "cratis/builder");

    [Fact] void should_key_by_the_repository() => _result.ShouldEqual("dockerhub:cratis/builder");
}
