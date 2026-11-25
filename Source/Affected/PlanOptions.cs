// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The inputs of a selection.
/// </summary>
/// <param name="From">The ref the change is measured against, normally the default branch; the merge base with <paramref name="To"/> is used.</param>
/// <param name="To">The ref being tested.</param>
/// <param name="Mode">The situation the tests run in.</param>
/// <param name="Level">How finely tests are selected.</param>
/// <param name="ForceAll">Whether a full run is forced, for example by a pull request label.</param>
/// <param name="Discover">Whether to build affected test projects and discover their units, which detects new tests and verifies filters.</param>
public record PlanOptions(
    string From,
    string To,
    PlanMode Mode = PlanMode.PullRequest,
    SelectionLevel Level = SelectionLevel.Test,
    bool ForceAll = false,
    bool Discover = true);
