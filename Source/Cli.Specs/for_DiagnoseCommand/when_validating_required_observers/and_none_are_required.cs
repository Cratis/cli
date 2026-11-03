// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

public class and_none_are_required : Specification
{
    string? _error;

    void Because() => _error = DiagnoseCommand.ValidateRequiredObservers(new DiagnoseSettings { AllNamespaces = true, AllEventStores = true });

    [Fact] void should_preserve_existing_scope_behavior() => _error.ShouldBeNull();
}
