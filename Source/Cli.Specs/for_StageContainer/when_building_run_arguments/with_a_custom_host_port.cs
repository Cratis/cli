// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_a_custom_host_port : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArguments("/work/space", "1.2.0", 9191);

    [Fact] void should_publish_the_custom_host_port_to_the_api_port() => _arguments.ShouldContain("9191:9090");
    [Fact] void should_run_the_requested_tag() => _arguments.ShouldContain("cratis/stage:1.2.0");
}
