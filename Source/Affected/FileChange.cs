// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// A change to one file between two revisions. Paths are repository relative with forward slashes.
/// </summary>
/// <param name="Kind">The kind of change.</param>
/// <param name="Path">The path in the later revision, or the deleted path for a deletion.</param>
/// <param name="OldPath">The path in the earlier revision when the file was renamed.</param>
public record FileChange(ChangeKind Kind, string Path, string? OldPath = null)
{
    /// <summary>
    /// Gets every path this change touched: the new path and, for a rename, the old one.
    /// </summary>
    public IEnumerable<string> Paths => OldPath is null ? [Path] : [Path, OldPath];
}
