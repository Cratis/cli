// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_updating_without_writing_or_with_nothing_newer : Specification
{
    ExtensionProject _dryRun;
    ExtensionProject _current;
    ExtensionProject _noSuchSystem;
    ExtensionReport _dryRunReport;
    ExtensionReport _currentReport;
    Exception _unknownSystem;
    string _before;

    void Establish()
    {
        _dryRun = new();
        _dryRun.AllowPublisher();
        var pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _dryRun.Registry.With(pinned, ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe3WithRefund));
        _dryRun.Lock("2", pinned);
        _dryRun.Cache.Store(pinned.Digest, pinned.Contract);
        _before = _dryRun.LockText;

        _current = new();
        _current.AllowPublisher();
        _current.Registry.With(pinned);
        _current.Lock("2", pinned);
        _noSuchSystem = _current;
    }

    async Task Because()
    {
        _dryRunReport = await _dryRun.Service().Update(_dryRun.Workspace(), "Payments", true, false, CancellationToken.None);
        _currentReport = await _current.Service().Update(_current.Workspace(), null, false, false, CancellationToken.None);
        _unknownSystem = await Catch.Exception(() => _noSuchSystem.Service().Update(_noSuchSystem.Workspace(), "Ledger", false, false, CancellationToken.None));
    }

    void Destroy()
    {
        _dryRun.Dispose();
        _current.Dispose();
    }

    [Fact] void should_report_the_change_in_a_dry_run() => _dryRunReport.Changes.Single().To.ShouldEqual("2.4.0");
    [Fact] void should_not_call_a_dry_run_update_updated() => _dryRunReport.Outcomes.Single().Status.ShouldEqual("would-update");
    [Fact] void should_not_write_the_lock_in_a_dry_run() => _dryRun.LockText.ShouldEqual(_before);
    [Fact] void should_not_report_the_lock_as_written_in_a_dry_run() => _dryRunReport.LockChanged.ShouldBeFalse();
    [Fact] void should_report_a_reference_with_nothing_newer_as_unchanged() => _currentReport.Outcomes.Single().Status.ShouldEqual("unchanged");
    [Fact] void should_report_no_change_when_nothing_is_newer() => _currentReport.Changes.ShouldBeEmpty();
    [Fact] void should_refuse_a_system_that_names_no_extension() => ((ExtensionFailure)_unknownSystem).Kind.ShouldEqual(ExtensionFailureKind.Unresolvable);
}
