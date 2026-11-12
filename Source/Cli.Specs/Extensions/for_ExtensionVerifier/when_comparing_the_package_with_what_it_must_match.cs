// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Extensions.Verification;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_comparing_the_package_with_what_it_must_match : given.a_valid_package
{
    ExtensionVerifyReport _matching = null!;
    ExtensionVerifyReport _otherDigest = null!;
    ExtensionVerifyReport _otherContract = null!;
    ExtensionVerifyReport _newerCompiler = null!;
    ExtensionVerifyReport _dropIn = null!;
    ExtensionVerifyReport _notADropIn = null!;

    ExtensionLockEntry Entry(string digest, string hash) => new("cratis/demo", "latest", "ghcr.io/cratis/demo", "1.0.0", digest, hash, "cratis");

    async Task Because()
    {
        var hash = ExtensionHashes.Of(Encoding.UTF8.GetBytes(Contract));
        _matching = await Run(new(Entry("sha256:aa", hash), "sha256:aa", null, "6.22.0"));
        _otherDigest = await Run(new(Entry("sha256:aa", hash), "sha256:bb", null, "6.22.0"));
        _otherContract = await Run(new(Entry("sha256:aa", "sha256:00"), "sha256:aa", null, "6.22.0"));

        ChangeContract("requires stage \"^5\"", "requires stage \"^5\"\n  requires screenplay \"^99\"");
        _newerCompiler = await Run();

        var baseline = ExtensionContractParser.Parse(Contract).Value;
        Write("contract.play", Contract);
        _dropIn = await Run(new(null, null, baseline, "6.22.0"));
        ChangeContract("  ability Ping idempotent\n    text String\n", "  ability Pong idempotent\n    text String\n");
        _notADropIn = await Run(new(null, null, baseline, "6.22.0"));
    }

    [Fact] void should_pass_a_package_the_lock_pins() => _matching.Checks.Single(check => check.Check == "digests").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_fail_a_package_with_another_digest() => _otherDigest.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ExtensionVerifyCodes.DigestMismatch);
    [Fact] void should_fail_a_contract_with_another_hash() => _otherContract.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ExtensionVerifyCodes.ContractHashMismatch);
    [Fact] void should_fail_a_package_that_requires_a_newer_compiler() => _newerCompiler.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ExtensionVerifyCodes.ScreenplayRequirementNotMet);
    [Fact] void should_pass_a_drop_in_provider() => _dropIn.Checks.Single(check => check.Check == "compatibility").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_fail_a_package_that_no_longer_offers_what_the_baseline_did() => _notADropIn.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ExtensionVerifyCodes.IncompatibleWithBaseline);
}
