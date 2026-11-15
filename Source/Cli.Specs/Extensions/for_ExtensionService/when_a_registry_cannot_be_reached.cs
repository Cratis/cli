// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_a_registry_cannot_be_reached : Specification
{
    ExtensionProject _project;
    ExtensionReport _unreachable;
    ExtensionReport _refused;
    ExtensionReport _pinnedUnreachable;
    string _before;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
    }

    async Task Because()
    {
        _project.Registry.Fails = ExtensionFailureKind.Unreachable;
        _unreachable = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);
        _project.Registry.Fails = ExtensionFailureKind.Unauthorized;
        _refused = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);
        _project.Lock("2", ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
        _before = _project.LockText;
        _project.Registry.Fails = ExtensionFailureKind.Unreachable;
        _pinnedUnreachable = await _project.Service().Restore(_project.Workspace(), true, CancellationToken.None);
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_report_the_failure_kind() => _unreachable.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unreachable);
    [Fact] void should_name_the_reference_as_the_model_writes_it() => _unreachable.Outcomes.Single().Reference.ShouldEqual("cratis/stripe");
    [Fact] void should_report_refused_credentials() => _refused.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unauthorized);
    [Fact] void should_fail_a_pinned_reference_that_cannot_be_fetched() => _pinnedUnreachable.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unreachable);
    [Fact] void should_not_write_a_lock_when_nothing_could_be_resolved() => _unreachable.LockChanged.ShouldBeFalse();
    [Fact] void should_leave_an_existing_lock_alone() => _project.LockText.ShouldEqual(_before);
}
