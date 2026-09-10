// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the relocation-safe source-path policy used for one project.
/// </summary>
/// <param name="LogicalProjectPath">The workspace-relative logical project path.</param>
/// <param name="ProjectIdentity">The stable project identity.</param>
/// <param name="PolicyVersion">The source-path policy version.</param>
/// <param name="DisplayRoot">Whether source locations display from the project or workspace root.</param>
/// <param name="CasePolicy">The case policy used for stable source identities.</param>
public record ScreenplaySourcePolicyProvenance(
    string LogicalProjectPath,
    string ProjectIdentity,
    int PolicyVersion,
    string DisplayRoot,
    string CasePolicy);
