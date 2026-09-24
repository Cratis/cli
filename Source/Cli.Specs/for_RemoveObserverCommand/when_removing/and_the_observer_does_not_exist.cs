// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_RemoveObserverCommand.when_removing;

public class and_the_observer_does_not_exist : given.a_remove_observer_command
{
    int _result;

    void Establish() => Removal(ObserverRemovalOutcome.ObserverNotFound);

    async Task Because() => _result = await Execute();

    [Fact] void should_report_it_as_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
}
