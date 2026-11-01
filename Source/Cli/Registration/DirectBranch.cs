// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy
namespace Cratis.Cli.Registration;

/// <summary>Authentication for the Direct capability API.</summary>
[CliBranch("direct", "Sign in to Direct and manage the active tenant")]
public static class DirectBranch
{
    /// <summary>The stdio bridge to Direct's MCP server and its registration in AI clients.</summary>
    [CliBranch("mcp", "Run the stdio bridge to Direct's MCP server, or register it in AI clients with install, status and uninstall")]
    public static class Mcp;
}
