// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_locked_with_a_reference_the_lock_does_not_pin : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    ExtensionReport _staleRange;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _project.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    async Task Because()
    {
        _report = await _project.Service().Restore(_project.Workspace(), true, CancellationToken.None);
        _project.Lock("1", ExtensionPackageFactory.Signed("1.0.0", ExtensionContracts.Stripe2));
        _staleRange = await _project.Service().Restore(_project.Workspace(), true, CancellationToken.None);
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_fail_as_not_locked() => _report.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.NotLocked);
    [Fact] void should_fail_a_lock_written_for_another_range_the_same_way() => _staleRange.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.NotLocked);
    [Fact] void should_not_resolve_anything() => _project.Registry.Pulls.ShouldEqual(0);
    [Fact] void should_leave_the_stale_lock_as_it_was() => _project.LockText.ShouldContain("range \"1\"");
    [Fact] void should_not_create_the_lock_when_there_is_none() => _report.LockChanged.ShouldBeFalse();
}
