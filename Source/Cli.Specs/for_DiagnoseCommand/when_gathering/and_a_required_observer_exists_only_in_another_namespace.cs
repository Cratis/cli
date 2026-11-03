// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_exists_only_in_another_namespace : given.required_observer_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Is<AllObserversRequest>(request => request.Namespace == "tenant-two"))
        .Returns(Task.FromResult<IEnumerable<ObserverInformation>>([new ObserverInformation { Id = "required", RunningState = ObserverRunningState.Active }]));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_match_another_namespace() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Absent);
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
