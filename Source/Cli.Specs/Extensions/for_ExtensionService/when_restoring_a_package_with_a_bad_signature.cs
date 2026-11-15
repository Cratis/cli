// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_a_package_with_a_bad_signature : Specification
{
    ExtensionProject _forged;
    ExtensionProject _rotated;
    ExtensionReport _forgedReport;
    ExtensionReport _rotatedReport;

    void Establish()
    {
        _forged = new();
        _forged.AllowPublisher();
        _forged.Registry.With(ExtensionPackageFactory.Forged("2.3.1", ExtensionContracts.Stripe2));
        _rotated = new();
        new ExtensionAllowList([]).Add(_rotated.Root, new("https://github.com/cratis", ExtensionPackageFactory.FingerprintOf("https://github.com/someone-else")));
        _rotated.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    async Task Because()
    {
        _forgedReport = await _forged.Service().Restore(_forged.Workspace(), false, CancellationToken.None);
        _rotatedReport = await _rotated.Service().Restore(_rotated.Workspace(), false, CancellationToken.None);
    }

    void Destroy()
    {
        _forged.Dispose();
        _rotated.Dispose();
    }

    [Fact] void should_fail_a_forged_signature() => _forgedReport.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.SignatureInvalid);
    [Fact] void should_not_pin_a_forged_package() => _forged.LockText.ShouldBeEmpty();
    [Fact] void should_fail_a_publisher_whose_key_is_not_the_one_the_allow_list_records() => _rotatedReport.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.SignatureInvalid);
    [Fact] void should_say_how_to_review_the_key_change() => _rotatedReport.Outcomes.Single().Message.ShouldContain("allow-list");
}
