// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_a_reference_the_lock_does_not_pin : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    RegistryPackage _newest;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _newest = ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe2);
        _project.Registry.With(
            ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2),
            _newest,
            ExtensionPackageFactory.Signed("3.0.0", ExtensionContracts.Stripe3WithRefund));
    }

    async Task Because() => _report = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_succeed() => _report.Succeeded.ShouldBeTrue();
    [Fact] void should_pin_the_newest_release_in_the_range() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Version.ShouldEqual("2.4.0");
    [Fact] void should_record_the_digest() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Digest.ShouldEqual(_newest.Digest);
    [Fact] void should_record_the_contract_hash() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.ContractHash.ShouldEqual(ExtensionHashes.Of(_newest.Contract));
    [Fact] void should_record_the_publisher() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Publisher.ShouldEqual("https://github.com/cratis");
    [Fact] void should_record_the_range() => ExtensionLock.Parse(_project.LockText).Value!.Find("cratis/stripe")!.Range.ShouldEqual("2");
    [Fact] void should_fill_the_cache() => _project.Cache.TryRead(_newest.Digest).ShouldNotBeNull();
    [Fact] void should_report_the_lock_as_written() => _report.LockChanged.ShouldBeTrue();
}
