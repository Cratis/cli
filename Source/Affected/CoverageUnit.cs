// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// One test unit of a coverage map.
/// </summary>
/// <param name="Name">The class or test name.</param>
/// <param name="Covers">Sorted indexes into the file table of the source files it executed.</param>
/// <param name="DeclaredIn">Sorted indexes into the file table of the source files that declare it.</param>
public record CoverageUnit(string Name, IReadOnlyList<int> Covers, IReadOnlyList<int> DeclaredIn);
