// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_a_reference_no_release_satisfies : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    ExtensionReport _missingPin;

    void Establish()
    {
        _project = new("cratis/stripe", "9");
        _project.AllowPublisher();
        _project.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    async Task Because()
    {
        _report = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);
        var pinned = ExtensionPackageFactory.Signed("1.0.0", ExtensionContracts.Stripe2);
        _project.Lock("9", pinned);
        _missingPin = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_fail_as_unresolvable() => _report.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unresolvable);
    [Fact] void should_name_the_range_in_the_message() => _report.Outcomes.Single().Message.ShouldContain("'9'");
    [Fact] void should_not_write_a_lock() => _report.LockChanged.ShouldBeFalse();
    [Fact] void should_fail_a_pinned_release_the_registry_no_longer_has_as_unresolvable() => _missingPin.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unresolvable);
}
