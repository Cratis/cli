// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_FastForwardObserverCommand.when_fast_forwarding;

public class and_the_observer_is_already_at_the_tail : given.a_fast_forward_observer_command
{
    int _result;

    void Establish() => Responds(FastForwardOutcome.AlreadyAtTail);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
}
