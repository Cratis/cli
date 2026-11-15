// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_locked_with_a_complete_lock : Specification
{
    ExtensionProject _project;
    ExtensionReport _report;
    RegistryPackage _pinned;
    string _before;
    DateTime _writtenBefore;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Registry.With(_pinned, ExtensionPackageFactory.Signed("2.4.0", ExtensionContracts.Stripe3WithRefund));
        _project.Lock("2", _pinned);
        _before = _project.LockText;
        _writtenBefore = File.GetLastWriteTimeUtc(_project.LockPath);
    }

    async Task Because() => _report = await _project.Service().Restore(_project.Workspace(), true, CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_succeed() => _report.Succeeded.ShouldBeTrue();
    [Fact] void should_fill_the_cache() => _project.Cache.TryRead(_pinned.Digest).ShouldNotBeNull();
    [Fact] void should_leave_the_lock_text_alone() => _project.LockText.ShouldEqual(_before);
    [Fact] void should_not_touch_the_lock_file() => File.GetLastWriteTimeUtc(_project.LockPath).ShouldEqual(_writtenBefore);
    [Fact] void should_not_report_the_lock_as_written() => _report.LockChanged.ShouldBeFalse();
}
