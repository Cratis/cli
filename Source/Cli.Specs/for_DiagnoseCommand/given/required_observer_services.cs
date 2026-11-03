// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class required_observer_services : healthy_services
{
    protected DiagnoseObserverRequirement Requirement => _data.ObserverRequirements.Single();

    void Establish() => _settings.RequireObserver = ["required"];

    protected void SetObserver(ObserverRunningState state) => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>())
        .Returns(Task.FromResult<IEnumerable<ObserverInformation>>([new ObserverInformation { Id = "required", RunningState = state }]));
}
