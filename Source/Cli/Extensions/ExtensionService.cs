// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Restores, updates and vendors the extensions a model references. The one restore code path: <c language="csharp">cratis run</c> and the language server use it too.
/// </summary>
/// <param name="packages">Finds and checks packages.</param>
/// <param name="cache">The digest-keyed cache the compiler reads.</param>
public class ExtensionService(ExtensionPackages packages, ExtensionCache cache)
{
    /// <summary>
    /// Fills the cache from the lock. A reference the lock does not pin yet is resolved and pinned, unless <paramref name="locked"/>.
    /// </summary>
    /// <param name="workspace">The model root.</param>
    /// <param name="locked">Whether the lock must already pin every reference; the lock is then never written.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    public async Task<ExtensionReport> Restore(ExtensionWorkspace workspace, bool locked, CancellationToken cancellationToken)
    {
        var current = workspace.Lock;
        var outcomes = new List<ExtensionOutcome>();
        foreach (var reference in workspace.References.DistinctBy(item => item.Reference))
        {
            try
            {
                var entry = current.Find(reference.Reference);
                if (entry is null || entry.Range != reference.Range)
                {
                    if (locked) throw new ExtensionFailure(ExtensionFailureKind.NotLocked, reference.Reference, $"'{reference.Reference}' is not pinned for version '{reference.Range}' in {ExtensionLock.FileName}; run 'cratis extension restore' and commit the lock.");
                    var package = await packages.Resolve(reference, null, cancellationToken);
                    cache.Store(package.Digest, package.Contract);
                    entry = ExtensionPackages.EntryFor(reference, package);
                    current = current.With(entry);
                    outcomes.Add(Outcome(entry, "pinned"));
                }
                else
                {
                    outcomes.Add(Outcome(entry, await Fill(entry, cancellationToken) ? "restored" : "cached"));
                }
            }
            catch (ExtensionFailure failure)
            {
                outcomes.Add(Failed(failure));
            }
        }

        return new(outcomes, [], !locked && workspace.Save(current));
    }

    /// <summary>
    /// Moves references to the newest release within their range and reports how each contract changes.
    /// </summary>
    /// <param name="workspace">The model root.</param>
    /// <param name="system">The system to update, or <see langword="null"/> for every reference.</param>
    /// <param name="dryRun">Whether to report without writing the lock.</param>
    /// <param name="allowIncompatible">Whether to write the lock although a new contract no longer offers what the model uses.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    /// <exception cref="ExtensionFailure">Thrown when the system does not name an extension.</exception>
    public async Task<ExtensionReport> Update(ExtensionWorkspace workspace, string? system, bool dryRun, bool allowIncompatible, CancellationToken cancellationToken)
    {
        var current = workspace.Lock;
        var outcomes = new List<ExtensionOutcome>();
        var changes = new List<ExtensionChange>();
        var targets = workspace.References.Where(item => system is null || item.System == system).DistinctBy(item => item.Reference).ToArray();
        if (system is not null && targets.Length == 0) throw new ExtensionFailure(ExtensionFailureKind.Unresolvable, system, $"System '{system}' does not name an extension.");
        foreach (var reference in targets)
        {
            var old = workspace.Lock.Find(reference.Reference);
            try
            {
                var package = await packages.Resolve(reference, old?.Publisher, cancellationToken);
                var entry = ExtensionPackages.EntryFor(reference, package);
                if (old is not null && old.Digest == entry.Digest)
                {
                    outcomes.Add(Outcome(entry, "unchanged"));
                    continue;
                }

                var change = ExtensionChanges.Describe(workspace, cache, old, entry, package);
                changes.Add(change);
                if (change.IncompatibleUses.Count > 0 && !allowIncompatible)
                {
                    outcomes.Add(Outcome(entry, "failed") with { Failure = ExtensionFailureKind.Incompatible, Message = $"'{reference.Reference}' {entry.Version} no longer offers what the model uses ({change.IncompatibleUses.Count} use(s)); the lock was not changed for it." });
                    continue;
                }

                cache.Store(package.Digest, package.Contract);
                current = current.With(entry);
                outcomes.Add(Outcome(entry, dryRun ? "would-update" : "updated"));
            }
            catch (ExtensionFailure failure)
            {
                outcomes.Add(Failed(failure));
            }
        }

        return new(outcomes, changes, !dryRun && workspace.Save(current));
    }

    /// <summary>
    /// Copies every locked artifact to a destination.
    /// </summary>
    /// <param name="workspace">The model root.</param>
    /// <param name="destination">Where to copy to.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    public async Task<ExtensionReport> Vendor(ExtensionWorkspace workspace, IExtensionDestination destination, CancellationToken cancellationToken)
    {
        var outcomes = new List<ExtensionOutcome>();
        foreach (var reference in workspace.References.DistinctBy(item => item.Reference).Where(item => workspace.Lock.Find(item.Reference)?.Range != item.Range))
        {
            outcomes.Add(Failed(new ExtensionFailure(ExtensionFailureKind.NotLocked, reference.Reference, $"'{reference.Reference}' is not pinned for version '{reference.Range}' in {ExtensionLock.FileName}; restore first.")));
        }

        foreach (var entry in workspace.Lock.Entries)
        {
            try
            {
                var package = await packages.Fetch(entry, cancellationToken);
                await destination.Push(package, cancellationToken);
                outcomes.Add(Outcome(entry, "vendored"));
            }
            catch (ExtensionFailure failure)
            {
                outcomes.Add(Failed(failure));
            }
        }

        return new(outcomes, [], false);
    }

    static ExtensionOutcome Outcome(ExtensionLockEntry entry, string status) => new(entry.Reference, status, entry.Version, entry.Digest, entry.Publisher);

    static ExtensionOutcome Failed(ExtensionFailure failure) => new(failure.Reference, "failed", Failure: failure.Kind, Message: failure.Message);

    async Task<bool> Fill(ExtensionLockEntry entry, CancellationToken cancellationToken)
    {
        if (cache.TryRead(entry.Digest) is { } cached && ExtensionHashes.Of(cached) == entry.ContractHash) return false;
        var package = await packages.Fetch(entry, cancellationToken);
        cache.Store(entry.Digest, package.Contract);
        return true;
    }
}
