// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Commands for installing and maintaining the Cratis AI corpus and listing its profiles.
/// </summary>
[CliBranch("ai", "Install, update, inspect, and remove Cratis-managed AI guidance, and list the profiles it offers.")]
public static class AiBranch
{
    /// <summary>
    /// Profiles offered by the Cratis AI corpus.
    /// </summary>
    [CliBranch("profiles", "List the profiles the Cratis AI corpus offers.")]
    public static class Profiles;
}
