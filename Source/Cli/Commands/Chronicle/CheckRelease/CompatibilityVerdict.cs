// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// The verdict of a compatibility check, using the shared cratis.compat/v1 vocabulary.
/// </summary>
public static class CompatibilityVerdict
{
    /// <summary>
    /// Every check passed.
    /// </summary>
    public const string Compatible = "compatible";

    /// <summary>
    /// A check found the release cannot work against the target.
    /// </summary>
    public const string Incompatible = "incompatible";

    /// <summary>
    /// A check could not establish an answer. Never a pass.
    /// </summary>
    public const string Unknown = "unknown";
}
