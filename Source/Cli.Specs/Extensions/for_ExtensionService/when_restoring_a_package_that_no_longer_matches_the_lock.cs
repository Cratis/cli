// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_a_package_that_no_longer_matches_the_lock : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    ExtensionReport _otherPublisher;
    RegistryPackage _pinned;
    string _before;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _project.AllowPublisher("https://github.com/mallory");
        _pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Lock("2", _pinned);
        _before = _project.LockText;
        _project.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe3WithRefund));
    }

    async Task Because()
    {
        _report = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);
        var republished = new InMemoryExtensionRegistry().With(_pinned with { Signature = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2, "https://github.com/mallory").Signature });
        _otherPublisher = await new ExtensionProject().Service(null, republished).Restore(_project.Workspace(), false, CancellationToken.None);
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_fail_as_mismatched() => _report.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Mismatched);
    [Fact] void should_not_fill_the_cache_with_the_changed_package() => _project.Cache.TryRead(_pinned.Digest).ShouldBeNull();
    [Fact] void should_leave_the_lock_alone() => _project.LockText.ShouldEqual(_before);
    [Fact] void should_fail_a_package_republished_under_another_publisher_as_mismatched() => _otherPublisher.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Mismatched);
}
