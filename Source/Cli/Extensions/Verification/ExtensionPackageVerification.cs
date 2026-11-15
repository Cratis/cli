// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Verifies a package folder or a package a registry serves, so both are held to the same checks.
/// </summary>
/// <param name="verifier">The verifier.</param>
/// <param name="download">Fetches a package by reference, range and, when the lock pins it, the lock entry.</param>
public class ExtensionPackageVerification(ExtensionVerifier verifier, Func<string, string?, ExtensionLockEntry?, CancellationToken, Task<RegistryPackage>> download)
{
    /// <summary>
    /// Verifies a folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="options">What to compare the package with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    public Task<ExtensionVerifyReport> VerifyFolder(string folder, ExtensionVerifyOptions options, CancellationToken cancellationToken) =>
        verifier.Verify(folder, folder, options, cancellationToken);

    /// <summary>
    /// Fetches a package, lays it out as it ships and verifies it.
    /// </summary>
    /// <param name="reference">The package reference: a catalog short name, an OCI reference or an npm package.</param>
    /// <param name="range">The version range, or <see langword="null"/> for the latest release.</param>
    /// <param name="locked">The lock entry that pins the package, if there is one.</param>
    /// <param name="options">What to compare the package with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the package cannot be fetched or trusted.</exception>
    public async Task<ExtensionVerifyReport> VerifyReference(string reference, string? range, ExtensionLockEntry? locked, ExtensionVerifyOptions options, CancellationToken cancellationToken)
    {
        RegistryPackage package;
        try
        {
            package = await download(reference, range, locked, cancellationToken);
        }
        catch (ExtensionFailure failure) when (failure.Kind == ExtensionFailureKind.Mismatched && locked is not null)
        {
            var finding = new VerifyDiagnostic(ExtensionVerifyCodes.DigestsCheck, ExtensionVerifyCodes.DigestMismatch, VerifySeverity.Error, failure.Message);
            return new(reference, [.. ExtensionVerifyCodes.Checks.Select(check => check == ExtensionVerifyCodes.DigestsCheck ? new VerifyCheckResult(check, VerifyStatus.Failed, [finding]) : new VerifyCheckResult(check, VerifyStatus.NotApplicable, []))]);
        }

        var folder = Path.Combine(Path.GetTempPath(), "cratis-extension-verify", Guid.NewGuid().ToString("N"));
        try
        {
            ExtensionPackageExtractor.Extract(package, folder);
            return await verifier.Verify($"{package.Resolved} {package.Version}", folder, options with { PackageDigest = package.Digest, Locked = locked }, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
