// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

[Collection(CliSpecsCollection.Name)]
public class and_the_scope_is_explicit : given.required_observer_services
{
    string? _error;

    void Because() => _error = DiagnoseCommand.ValidateRequiredObservers(_settings);

    [Fact] void should_accept_the_selected_scope() => _error.ShouldBeNull();
}
