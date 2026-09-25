// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_RemoveObserverCommand.when_removing;

/// <summary>
/// A refusal has to exit non-zero. It comes back as a perfectly successful RPC that declined, so reporting the
/// outcome is the only thing that separates "the observer is gone" from "nothing happened" - and a script that only
/// looks at the exit code would otherwise carry on as though the store had been cleaned up.
/// </summary>
public class and_the_observer_is_still_running : given.a_remove_observer_command
{
    int _result;

    void Establish() => Removal(ObserverRemovalOutcome.ObserverActive, "the-busy-namespace");

    async Task Because() => _result = await Execute();

    [Fact] void should_not_report_success() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
