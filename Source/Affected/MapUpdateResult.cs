// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// What a merge-time map update did.
/// </summary>
/// <param name="Written">The repository relative map files that were created or changed.</param>
/// <param name="Deleted">The repository relative map files deleted because their test project is gone.</param>
/// <param name="FailedProjects">The test projects with failing tests; their maps were left as they were.</param>
public record MapUpdateResult(IReadOnlyList<string> Written, IReadOnlyList<string> Deleted, IReadOnlyList<string> FailedProjects);
