// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_watching;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_recovers : given.watch_services
{
    void Establish()
    {
        _settings.RequireObserver = ["required"];
        _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(async Task<IEnumerable<ObserverInformation>> (_) =>
        {
            _sweeps++;
            if (_sweeps == 2) await _cancellation.CancelAsync();
            return _sweeps == 2
                ? new List<ObserverInformation> { new() { Id = "required", RunningState = ObserverRunningState.Active } }
                : [];
        });
    }

    async Task Because() => _exitCode = await DiagnoseCommand.RunWatch(_services, _settings, _cancellation.Token);

    [Fact] void should_exit_with_the_final_recovery() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_complete_two_sweeps() => _sweeps.ShouldEqual(2);
}
