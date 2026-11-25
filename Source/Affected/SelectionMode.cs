// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// How one test project is run.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Every test runs.
    /// </summary>
    All,

    /// <summary>
    /// Only the selected entries run.
    /// </summary>
    Filtered,

    /// <summary>
    /// Nothing runs.
    /// </summary>
    Skip
}
