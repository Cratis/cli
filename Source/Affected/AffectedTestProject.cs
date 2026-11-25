// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// A test project that a change can reach.
/// </summary>
/// <param name="Path">The repository relative path of the project file.</param>
/// <param name="RunAll">Whether every test of it must run, whatever a coverage map says.</param>
/// <param name="Reason">Why it must run in full, or why it is affected.</param>
public record AffectedTestProject(string Path, bool RunAll, string Reason);
