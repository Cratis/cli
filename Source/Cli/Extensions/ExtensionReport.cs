// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents what happened to one extension reference.
/// </summary>
/// <param name="Reference">The reference as the model writes it.</param>
/// <param name="Status">One of <c language="csharp">cached</c>, <c language="csharp">restored</c>, <c language="csharp">pinned</c>, <c language="csharp">updated</c>, <c language="csharp">unchanged</c>, <c language="csharp">vendored</c> or <c language="csharp">failed</c>.</param>
/// <param name="Version">The version it is, or would be, locked at.</param>
/// <param name="Digest">The digest of the package.</param>
/// <param name="Publisher">The publisher recorded in the lock.</param>
/// <param name="Failure">Why it failed; <see langword="null"/> when it did not.</param>
/// <param name="Message">What went wrong. Never holds a credential.</param>
public record ExtensionOutcome(string Reference, string Status, string? Version = null, string? Digest = null, string? Publisher = null, ExtensionFailureKind? Failure = null, string? Message = null);

/// <summary>
/// Represents how a contract changes when a reference moves to a newer release.
/// </summary>
/// <param name="Reference">The reference as the model writes it.</param>
/// <param name="From">The locked version, <see langword="null"/> when the reference was not locked.</param>
/// <param name="To">The newest release in range.</param>
/// <param name="Added">What the new contract offers that the old did not, or offers differently.</param>
/// <param name="Removed">What the old contract offered that the new one no longer offers or offers differently.</param>
/// <param name="IncompatibleUses">The uses in the model the new contract does not offer.</param>
public record ExtensionChange(string Reference, string? From, string To, IReadOnlyList<string> Added, IReadOnlyList<string> Removed, IReadOnlyList<string> IncompatibleUses);

/// <summary>
/// Represents the result of an extension operation.
/// </summary>
/// <param name="Outcomes">One outcome per reference.</param>
/// <param name="Changes">The contract changes an update found.</param>
/// <param name="LockChanged">Whether the lock file was written.</param>
public record ExtensionReport(IReadOnlyList<ExtensionOutcome> Outcomes, IReadOnlyList<ExtensionChange> Changes, bool LockChanged)
{
    /// <summary>
    /// Gets a value indicating whether every reference succeeded.
    /// </summary>
    public bool Succeeded => Outcomes.All(outcome => outcome.Failure is null);
}
