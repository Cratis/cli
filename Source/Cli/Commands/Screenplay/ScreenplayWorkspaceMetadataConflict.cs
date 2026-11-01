// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The exception that is thrown when Screenplay workspace metadata is a link or has the wrong file type, so it cannot be trusted.
/// </summary>
/// <param name="problem">What is wrong with the metadata.</param>
public sealed class ScreenplayWorkspaceMetadataConflict(string problem)
    : Exception($"MetadataPathConflict: {problem}. Fix the folder before starting the Screenplay MCP server.");
