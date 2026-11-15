// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_updating_within_the_range : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    RegistryPackage _newest;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        var pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _newest = ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe3WithRefund);
        _project.Registry.With(pinned, _newest, ExtensionPackageFactory.Signed("3.0.0", ExtensionContracts.Stripe3WithRefund));
        _project.Lock("2", pinned);
        _project.Cache.Store(pinned.Digest, pinned.Contract);
    }

    async Task Because() => _report = await _project.Service().Update(_project.Workspace(), null, false, false, CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_succeed() => _report.Succeeded.ShouldBeTrue();
    [Fact] void should_move_the_lock_to_the_newest_release_in_range() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Version.ShouldEqual("2.4.0");
    [Fact] void should_pin_its_digest() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Digest.ShouldEqual(_newest.Digest);
    [Fact] void should_report_the_move() => (_report.Changes.Single().From + " -> " + _report.Changes.Single().To).ShouldEqual("2.3.1 -> 2.4.0");
    [Fact] void should_report_what_the_new_contract_adds() => _report.Changes.Single().Added.ShouldContain("ability 'RefundPayment' is new");
    [Fact] void should_report_nothing_removed() => _report.Changes.Single().Removed.ShouldBeEmpty();
    [Fact] void should_report_no_incompatible_use() => _report.Changes.Single().IncompatibleUses.ShouldBeEmpty();
    [Fact] void should_fill_the_cache_with_the_new_contract() => _project.Cache.TryRead(_newest.Digest).ShouldNotBeNull();
}
