// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_FastForwardObserverCommand.when_fast_forwarding;

public class and_the_fast_forward_is_accepted : given.a_fast_forward_observer_command
{
    int _result;

    void Establish() => Responds(FastForwardOutcome.Accepted);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_say_it_was_accepted() => _output.ToString().ShouldContain("accepted");
}
