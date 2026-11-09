// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Building slices of a Studio event model with a coding agent or a generator.
/// </summary>
[CliBranch("build", "Build slices of a Cratis Studio event model with a coding agent or a generator. Connect this machine to Studio as a builder, install a build kit, run the build loop, build an exported order offline, or connect a local agent to a board for modeling.")]
public static class BuildBranch
{
    /// <summary>
    /// Build kits: the description of a stack a builder builds with.
    /// </summary>
    [CliBranch("kit", "Work with build kits, the manifests that describe a stack to the builder.")]
    public static class Kit;
}
