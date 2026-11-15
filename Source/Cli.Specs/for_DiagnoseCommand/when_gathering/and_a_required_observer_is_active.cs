// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_active : given.required_observer_services
{
    void Establish() => SetObserver(ObserverRunningState.Active);

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_satisfy_the_requirement() => Requirement.ShouldEqual(new DiagnoseObserverRequirement("required", "store", "tenant-one", DiagnoseObserverRequirementStatus.Satisfied, "Active", "running"));
    [Fact] void should_include_the_requirement_in_the_scope() => _data.Scopes.Single().ObserverRequirements.ShouldContainOnly(Requirement);
    [Fact] void should_be_healthy() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_query_the_selected_scope_once() => _services.Observers.Received(1).GetObservers(Arg.Is<AllObserversRequest>(request => request.EventStore == "store" && request.Namespace == "tenant-one"));
}
