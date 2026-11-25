// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The situation the tests are being run in.
/// </summary>
public enum PlanMode
{
    /// <summary>
    /// A pull request: only what the change can reach runs.
    /// </summary>
    PullRequest,

    /// <summary>
    /// The default branch after a merge: everything runs, with coverage attribution to refresh the maps.
    /// </summary>
    Merge,

    /// <summary>
    /// A scheduled run: everything runs.
    /// </summary>
    Nightly
}
