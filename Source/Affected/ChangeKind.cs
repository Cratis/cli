// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The kind of change a file went through between two revisions.
/// </summary>
public enum ChangeKind
{
    /// <summary>
    /// The file exists only in the later revision.
    /// </summary>
    Added,

    /// <summary>
    /// The file exists in both revisions with different content.
    /// </summary>
    Modified,

    /// <summary>
    /// The file exists only in the earlier revision.
    /// </summary>
    Deleted,

    /// <summary>
    /// The file moved from another path.
    /// </summary>
    Renamed
}
