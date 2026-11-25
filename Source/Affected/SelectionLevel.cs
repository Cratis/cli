// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// How finely tests are selected.
/// </summary>
public enum SelectionLevel
{
    /// <summary>
    /// Whole test projects: affected ones run in full, the others are skipped. Needs no coverage maps.
    /// </summary>
    Project,

    /// <summary>
    /// Individual test units, from coverage maps. A project without a usable map runs in full.
    /// </summary>
    Test
}
