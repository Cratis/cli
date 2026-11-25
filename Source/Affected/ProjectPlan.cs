// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The decision for one test project together with the arguments that carry it out.
/// </summary>
/// <param name="Selection">The decision.</param>
/// <param name="FilterBatches">The runner arguments, one list per run, when the selection is filtered.</param>
/// <param name="Assembly">The built assembly, when the planner had to build it.</param>
public record ProjectPlan(ProjectSelection Selection, IReadOnlyList<IReadOnlyList<string>> FilterBatches, TestAssembly? Assembly = null);
