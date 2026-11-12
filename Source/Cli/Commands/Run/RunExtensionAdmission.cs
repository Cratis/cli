// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// One extension reference <c language="text">cratis run</c> refuses to start with.
/// </summary>
/// <param name="Reference">The reference the model writes.</param>
/// <param name="Kind">Why it is refused.</param>
/// <param name="Message">What to do about it.</param>
public record RunExtensionRefusal(string Reference, ExtensionFailureKind Kind, string Message);

/// <summary>
/// Decides, before anything is started, whether the deployment allows the packages a model's extensions resolved to. Naming a package in a model is never
/// a trust decision: only what the lock pins and the project's allow-list covers is run.
/// </summary>
public static class RunExtensionAdmission
{
    /// <summary>
    /// Checks every extension reference of a model against its lock and the allow-list. Nothing is fetched.
    /// </summary>
    /// <param name="references">The extension references of the model.</param>
    /// <param name="extensionLock">What the lock pins.</param>
    /// <param name="allowList">The project's allow-list.</param>
    /// <returns>One refusal for each reference that may not run; empty when all may.</returns>
    public static IReadOnlyList<RunExtensionRefusal> Check(IEnumerable<ExtensionReference> references, ExtensionLock extensionLock, ExtensionAllowList allowList) =>
    [
        .. references
            .DistinctBy(reference => reference.Reference, StringComparer.Ordinal)
            .Select(reference => Check(reference, extensionLock, allowList))
            .OfType<RunExtensionRefusal>()
    ];

    static RunExtensionRefusal? Check(ExtensionReference reference, ExtensionLock extensionLock, ExtensionAllowList allowList)
    {
        var entry = extensionLock.Find(reference.Reference);
        if (entry is null || entry.Range != reference.Range)
        {
            return new(reference.Reference, ExtensionFailureKind.NotLocked, $"'{reference.Reference}' is not pinned for version '{reference.Range}' in {ExtensionLock.FileName}. Run 'cratis extension restore' and commit the lock; 'cratis run' never resolves a range itself.");
        }

        var publisher = allowList.Find(entry.Publisher);
        if (publisher is null)
        {
            return new(reference.Reference, ExtensionFailureKind.Unallowed, $"The publisher '{entry.Publisher}' of '{reference.Reference}' is not on the allow-list in {ExtensionAllowList.FileName}. Run 'cratis extension restore' to review and allow it, or remove the extension from the model.");
        }

        return publisher.Covers(entry.Resolved)
            ? null
            : new(reference.Reference, ExtensionFailureKind.Unallowed, $"The allow-list does not let '{entry.Publisher}' supply '{entry.Resolved}'. Add the reference to its entry in .cratis/{ExtensionAllowList.FileName}, or remove the extension from the model.");
    }
}
