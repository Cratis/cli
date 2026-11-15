// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

[Collection(CliSpecsCollection.Name)]
public class and_all_event_stores_are_selected : given.required_observer_services
{
    string? _error;

    void Establish() => _settings.AllEventStores = true;

    void Because() => _error = DiagnoseCommand.ValidateRequiredObservers(_settings);

    [Fact] void should_reject_aggregation() => _error.ShouldEqual("--require-observer cannot be combined with --all-namespaces or --all-event-stores");
}
