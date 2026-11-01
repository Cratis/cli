// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class without_authentication : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = StageContainer.BuildRunArguments("/work/model", "latest", 9090, 35000, "sandbox");

    [Fact] void should_leave_the_sandbox_playing_a_persona() => _arguments.Any(_ => _.StartsWith("Stage__Sandbox__ForwardedIdentity", StringComparison.Ordinal)).ShouldBeFalse();
}
