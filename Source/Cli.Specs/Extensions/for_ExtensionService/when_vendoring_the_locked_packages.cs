// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_vendoring_the_locked_packages : Specification
{
    ExtensionProject _project;
    ExtensionProject _airGapped;
    ExtensionReport _report;
    ExtensionReport _restored;
    RegistryPackage _pinned;
    string _destination;
    VendoredExtensionStore _store;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Registry.With(_pinned);
        _project.Lock("2", _pinned);
        _destination = Path.Combine(_project.Root, "vendor");
        _store = new(_destination);
        _airGapped = new();
        _airGapped.AllowPublisher();
        _airGapped.Lock("2", _pinned);
    }

    async Task Because()
    {
        _report = await _project.Service().Vendor(_project.Workspace(), _store, CancellationToken.None);
        _airGapped.Registry.Fails = ExtensionFailureKind.Unreachable;
        _restored = await _airGapped.Service(null, new VendoredExtensionStore(_destination)).Restore(_airGapped.Workspace(), true, CancellationToken.None);
    }

    void Destroy()
    {
        _project.Dispose();
        _airGapped.Dispose();
    }

    [Fact] void should_succeed() => _report.Succeeded.ShouldBeTrue();
    [Fact] void should_report_the_package_as_vendored() => _report.Outcomes.Single().Status.ShouldEqual("vendored");
    [Fact] void should_copy_the_contract_where_the_compiler_reads_it() => File.Exists(Path.Combine(_destination, _pinned.Digest.Replace(':', '-'), "contract.extension")).ShouldBeTrue();
    [Fact] void should_copy_the_artifact_parts() => File.Exists(Path.Combine(_destination, _pinned.Digest.Replace(':', '-'), "parts", "manifest.json")).ShouldBeTrue();
    [Fact] void should_keep_the_signature_so_the_copy_can_be_verified_offline() => _store.Read(_pinned.Digest)!.Signature!.Verifies().ShouldBeTrue();
    [Fact] void should_not_change_the_lock() => ExtensionLock.Parse(_project.LockText).Value!.Entries.Single().Digest.ShouldEqual(_pinned.Digest);
    [Fact] void should_let_restore_succeed_from_the_copy_when_the_registry_is_unreachable() => _restored.Succeeded.ShouldBeTrue();
    [Fact] void should_fill_the_cache_from_the_copy() => _airGapped.Cache.TryRead(_pinned.Digest).ShouldNotBeNull();
    [Fact] void should_not_have_asked_the_registry() => _airGapped.Registry.Pulls.ShouldEqual(0);
}
