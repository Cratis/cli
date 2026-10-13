// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502, CA1034 // Marker types: intentionally empty and nested for branch hierarchy

namespace Cratis.Cli.Registration;

/// <summary>
/// Authoring and generation of Cratis Screenplay (<c language="csharp">.play</c>) documents.
/// </summary>
[CliBranch("screenplay", "Work with Cratis Screenplay (.play) documents. Generate a Screenplay from source code, check code conformance to an authored model, validate your documents, host the embedded MCP server, or manage its desktop distribution.")]
public static class ScreenplayBranch
{
    /// <summary>
    /// Desktop distribution of the Screenplay MCP server.
    /// </summary>
    [CliBranch("desktop", "Install, update, inspect, and remove the Screenplay MCP server in Claude Desktop and ChatGPT Desktop.")]
    public static class Desktop;
}
