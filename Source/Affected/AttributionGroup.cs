// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// How many units share one coverage run when the coverage map is built.
/// </summary>
public enum AttributionGroup
{
    /// <summary>
    /// Every unit runs alone in its own process. Exact, but a process per test class does not scale to thousands of classes.
    /// </summary>
    Unit,

    /// <summary>
    /// The units of one test namespace run together, a namespace ending at its first for_ segment. Every unit of the group
    /// is credited with the group's coverage, so a change selects the whole group. Selection stays safe, only less narrow.
    /// </summary>
    Namespace
}
