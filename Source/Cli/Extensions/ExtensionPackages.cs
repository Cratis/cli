// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Finds and checks the packages a model's references and a lock's entries name.
/// </summary>
/// <param name="origin">Where packages are published.</param>
/// <param name="source">A registry or folder to use instead of the origin, or <see langword="null"/>.</param>
/// <param name="vendored">The packages vendored beside the lock.</param>
/// <param name="trust">The trust checks.</param>
public class ExtensionPackages(IExtensionRegistry origin, IExtensionRegistry? source, VendoredExtensionStore vendored, ExtensionTrust trust)
{
    /// <summary>
    /// Creates the lock entry that pins a package.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <param name="package">The package.</param>
    /// <returns>The entry.</returns>
    public static ExtensionLockEntry EntryFor(ExtensionReference reference, RegistryPackage package) =>
        new(reference.Reference, reference.Range, package.Resolved, package.Version, package.Digest, ExtensionHashes.Of(package.Contract), package.Signature!.Publisher);

    /// <summary>
    /// Resolves a reference to the newest release in its range, and checks it is trusted.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <param name="lockedPublisher">The publisher the lock records for it, if any.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The package.</returns>
    /// <exception cref="ExtensionFailure">Thrown when no release satisfies the range or the package may not be used.</exception>
    public async Task<RegistryPackage> Resolve(ExtensionReference reference, string? lockedPublisher, CancellationToken cancellationToken)
    {
        var resolved = ExtensionCatalog.Resolve(reference.Reference);
        var registry = source ?? origin;
        var version = (await registry.Versions(resolved, cancellationToken))
            .Where(candidate => ExtensionVersionRange.Satisfies(reference.Range, candidate))
            .OrderDescending(Comparer<string>.Create(ExtensionVersionRange.Compare))
            .FirstOrDefault()
            ?? throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, reference.Reference, $"No release of '{reference.Reference}' ({resolved}) satisfies version '{reference.Range}'.");
        var package = await registry.Pull(resolved, version, cancellationToken)
            ?? throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, reference.Reference, $"'{resolved}' {version} could not be pulled.");
        trust.Check(reference.Reference, package, lockedPublisher);
        return package;
    }

    /// <summary>
    /// Fetches exactly what a lock entry pins and checks it is still that and still trusted.
    /// </summary>
    /// <param name="entry">The lock entry.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The package.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the package is missing, differs from the lock or may not be used.</exception>
    public async Task<RegistryPackage> Fetch(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        RegistryPackage? package = vendored.Read(entry.Digest);
        if (package is null && source is not null) package = await source.Pull(entry.Resolved, entry.Version, cancellationToken);
        package ??= await origin.Pull(entry.Resolved, entry.Version, cancellationToken);
        if (package is null)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, entry.Reference, $"'{entry.Reference}' {entry.Version} ({entry.Digest}) could not be fetched from its source.");
        }

        if (package.Digest != entry.Digest || ExtensionHashes.Of(package.Contract) != entry.ContractHash)
        {
            throw new ExtensionFailure(ExtensionFailureKind.Mismatched, entry.Reference, $"'{entry.Reference}' {entry.Version} is no longer what the lock pinned (expected {entry.Digest}, found {package.Digest}).");
        }

        trust.Check(entry.Reference, package, entry.Publisher);
        return package;
    }
}
