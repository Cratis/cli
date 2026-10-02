// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Represents where the documents a view shows came from.
/// </summary>
public enum ViewOrigin
{
    /// <summary>
    /// The documents were embedded in the built assembly, or in an assembly it references.
    /// </summary>
    Embedded = 0,

    /// <summary>
    /// The documents were generated from the project's source, in memory.
    /// </summary>
    Generated = 1
}
