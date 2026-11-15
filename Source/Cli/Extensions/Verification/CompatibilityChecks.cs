// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// The checks that compare the package with something else: what <c language="text">screenplay.lock</c> pins, the compiler that will read it
/// and the release it replaces.
/// </summary>
public static class CompatibilityChecks
{
    /// <summary>
    /// Compares the package with its lock entry.
    /// </summary>
    /// <param name="contract">The bytes of <c language="text">contract.play</c>.</param>
    /// <param name="options">The options.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<VerifyDiagnostic> Digests(byte[] contract, ExtensionVerifyOptions options)
    {
        const string check = ExtensionVerifyCodes.DigestsCheck;
        if (options.Locked is not { } locked) yield break;
        if (options.PackageDigest is not null && options.PackageDigest != locked.Digest)
        {
            yield return new(check, ExtensionVerifyCodes.DigestMismatch, VerifySeverity.Error, $"The package is {options.PackageDigest} but screenplay.lock pins {locked.Digest} for '{locked.Reference}'.");
        }

        var hash = ExtensionHashes.Of(contract);
        if (hash != locked.ContractHash)
        {
            yield return new(check, ExtensionVerifyCodes.ContractHashMismatch, VerifySeverity.Error, $"The contract hashes to {hash} but screenplay.lock pins {locked.ContractHash} for '{locked.Reference}'.", ExtensionVerifier.ContractFile);
        }
    }

    /// <summary>
    /// Checks that the compiler can use the package as a provider.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="options">The options.</param>
    /// <returns>The findings.</returns>
    public static IEnumerable<VerifyDiagnostic> Compatibility(ExtensionContract contract, ExtensionVerifyOptions options)
    {
        const string check = ExtensionVerifyCodes.CompatibilityCheck;
        if (options.CompilerVersion is { } version)
        {
            foreach (var requirement in contract.Requirements.Where(requirement => requirement.Kind == "screenplay" && !ExtensionVersionRange.Satisfies(requirement.Value, version)))
            {
                yield return new(check, ExtensionVerifyCodes.ScreenplayRequirementNotMet, VerifySeverity.Error, $"The contract requires Screenplay {requirement.Value} but the compiler is {version}.", ExtensionVerifier.ContractFile, requirement.Location.Line);
            }
        }

        if (options.Baseline is { } baseline)
        {
            foreach (var mismatch in ExtensionCompatibility.Mismatches(baseline, contract))
            {
                yield return new(check, ExtensionVerifyCodes.IncompatibleWithBaseline, VerifySeverity.Error, $"Not a drop-in provider for the baseline: {mismatch}");
            }
        }
    }
}
