// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_from_the_lock : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    RegistryPackage _pinned;
    string _before;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Registry.With(_pinned, ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe3WithRefund));
        _project.Lock("2", _pinned);
        _before = _project.LockText;
    }

    async Task Because() => _report = await _project.Service().Restore(_project.Workspace(), false, CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_succeed() => _report.Succeeded.ShouldBeTrue();
    [Fact] void should_fetch_exactly_the_pinned_release_into_the_cache() => ExtensionHashes.Of(_project.Cache.TryRead(_pinned.Digest)).ShouldEqual(ExtensionHashes.Of(_pinned.Contract));
    [Fact] void should_not_move_to_a_newer_release() => _project.LockText.ShouldEqual(_before);
    [Fact] void should_not_report_the_lock_as_written() => _report.LockChanged.ShouldBeFalse();
    [Fact] void should_report_the_reference_as_restored() => _report.Outcomes.Single().Status.ShouldEqual("restored");
}
