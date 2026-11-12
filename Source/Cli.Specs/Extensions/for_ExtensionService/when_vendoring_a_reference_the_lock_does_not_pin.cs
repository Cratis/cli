// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_vendoring_a_reference_the_lock_does_not_pin : Specification
{
    ExtensionProject _project;
    ExtensionProject _unsigned;
    ExtensionReport _report;
    ExtensionReport _unsignedReport;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _project.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
        _unsigned = new();
        var package = ExtensionPackageFactory.Unsigned("2.3.1", ExtensionContracts.Stripe2);
        _unsigned.Registry.With(package);
        _unsigned.Lock("2", package);
    }

    async Task Because()
    {
        _report = await _project.Service().Vendor(_project.Workspace(), new VendoredExtensionStore(Path.Combine(_project.Root, "vendor")), CancellationToken.None);
        _unsignedReport = await _unsigned.Service().Vendor(_unsigned.Workspace(), new VendoredExtensionStore(Path.Combine(_unsigned.Root, "vendor")), CancellationToken.None);
    }

    void Destroy()
    {
        _project.Dispose();
        _unsigned.Dispose();
    }

    [Fact] void should_fail_as_not_locked() => _report.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.NotLocked);
    [Fact] void should_copy_nothing() => Directory.Exists(Path.Combine(_project.Root, "vendor")).ShouldBeFalse();
    [Fact] void should_refuse_to_vendor_a_package_that_is_not_signed() => _unsignedReport.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.NotSigned);
}
