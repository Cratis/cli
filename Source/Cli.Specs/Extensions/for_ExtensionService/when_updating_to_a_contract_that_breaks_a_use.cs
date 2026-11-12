// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_updating_to_a_contract_that_breaks_a_use : Specification
{
    ExtensionProject _blocked;
    ExtensionProject _forced;
    ExtensionReport _blockedReport;
    ExtensionReport _forcedReport;
    string _before;

    static ExtensionProject Project()
    {
        var project = new ExtensionProject();
        project.AllowPublisher();
        var pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        project.Registry.With(pinned, ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe3WithoutTrigger));
        project.Lock("2", pinned);
        project.Cache.Store(pinned.Digest, pinned.Contract);
        return project;
    }

    void Establish()
    {
        _blocked = Project();
        _forced = Project();
        _before = _blocked.LockText;
    }

    async Task Because()
    {
        _blockedReport = await _blocked.Service().Update(_blocked.Workspace(), null, false, false, CancellationToken.None);
        _forcedReport = await _forced.Service().Update(_forced.Workspace(), null, false, true, CancellationToken.None);
    }

    void Destroy()
    {
        _blocked.Dispose();
        _forced.Dispose();
    }

    [Fact] void should_report_the_use_the_new_contract_no_longer_offers() => _blockedReport.Changes.Single().IncompatibleUses.ShouldNotBeEmpty();
    [Fact] void should_report_what_the_new_contract_no_longer_offers() => _blockedReport.Changes.Single().Removed.ShouldContain("trigger 'PaymentSucceeded' is not offered");
    [Fact] void should_refuse_to_move_the_reference() => _blockedReport.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Incompatible);
    [Fact] void should_leave_the_lock_alone() => _blocked.LockText.ShouldEqual(_before);
    [Fact] void should_write_the_lock_when_incompatible_updates_are_allowed() => ExtensionLock.Parse(_forced.LockText).Value!.Find("cratis/stripe")!.Version.ShouldEqual("2.4.0");
    [Fact] void should_still_report_the_use_when_allowed() => _forcedReport.Changes.Single().IncompatibleUses.ShouldNotBeEmpty();
}
