// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// What to select when a changed file is unknown to a coverage map, which is always true of an added file.
/// Convention and reflection based discovery can make such a file affect tests that cover nothing that changed.
/// </summary>
public enum AddedFilePolicy
{
    /// <summary>
    /// Select every unit that covers any file of the same project.
    /// </summary>
    Project,

    /// <summary>
    /// Select every unit that covers any file of the same directory.
    /// </summary>
    Directory,

    /// <summary>
    /// Select nothing for it.
    /// </summary>
    Ignore
}
