// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Describes how a contract changes when a reference moves to another release.
/// </summary>
public static class ExtensionChanges
{
    /// <summary>
    /// Compares the locked contract with the new one, and the model's uses with the new one.
    /// </summary>
    /// <param name="workspace">The model root.</param>
    /// <param name="cache">The cache holding the locked contract.</param>
    /// <param name="old">The current entry, or <see langword="null"/> when the reference is not locked.</param>
    /// <param name="entry">The entry the reference would move to.</param>
    /// <param name="package">The new package.</param>
    /// <returns>The change.</returns>
    public static ExtensionChange Describe(ExtensionWorkspace workspace, ExtensionCache cache, ExtensionLockEntry? old, ExtensionLockEntry entry, RegistryPackage package)
    {
        var offered = Parse(package.Contract);
        var previous = old is null ? null : ParsePrevious(workspace, cache, old);
        var added = previous is null || offered is null ? [] : ExtensionCompatibility.Mismatches(offered, previous);
        var removed = previous is null || offered is null ? [] : ExtensionCompatibility.Mismatches(previous, offered);
        return new(entry.Reference, old?.Version, entry.Version, added, removed, offered is null ? [] : IncompatibleUses(workspace, cache, entry, package));
    }

    static ExtensionContract? Parse(byte[] contract) => ExtensionContractParser.Parse(ExtensionCache.Decode(contract)).Value;

    static ExtensionContract? ParsePrevious(ExtensionWorkspace workspace, ExtensionCache cache, ExtensionLockEntry old)
    {
        var bytes = cache.TryRead(old.Digest) ?? new ExtensionCache(workspace.VendorFolder).TryRead(old.Digest);
        return bytes is null ? null : Parse(bytes);
    }

    static IReadOnlyList<string> IncompatibleUses(ExtensionWorkspace workspace, ExtensionCache cache, ExtensionLockEntry entry, RegistryPackage package)
    {
        var temporary = Path.Combine(Path.GetTempPath(), "cratis-extension-" + Guid.NewGuid().ToString("N"));
        try
        {
            var candidate = new ExtensionCache(temporary);
            candidate.Store(package.Digest, package.Contract);
            var references = workspace.References.Select(item => item.Syntax).DistinctBy(item => item.Reference).ToArray();
            var resolver = new ExtensionContractResolver(candidate, ExtensionResolutionMode.Strict, workspace.Lock.With(entry), workspace.Root);
            var resolution = resolver.Resolve(references, workspace.Lock.With(entry), null, [candidate, cache, new ExtensionCache(workspace.VendorFolder)]);
            return [.. new ExtensionUseSites(resolution).Check(workspace.Application).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")];
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
        }
    }
}
