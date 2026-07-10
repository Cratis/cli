// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy

namespace Cratis.Cli.Registration;

/// <summary>
/// Arc application commands branch. Contains sub-branches for commands and queries.
/// </summary>
[CliBranch("arc", "Commands for interacting with a Cratis Arc application. Contains sub-branches for introspecting registered commands and queries.")]
public static class ArcBranch
{
    /// <summary>Command introspection.</summary>
    [CliBranch("commands", "Introspect registered command endpoints in the Arc application. List all commands with their routes, namespaces, and documentation.")]
    public static class Commands;

    /// <summary>Query introspection.</summary>
    [CliBranch("queries", "Introspect registered query endpoints in the Arc application. List all queries with their routes, namespaces, and documentation.")]
    public static class Queries;
}
