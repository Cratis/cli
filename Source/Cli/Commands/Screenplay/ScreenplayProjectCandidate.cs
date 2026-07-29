// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents a project in a loaded solution that a Screenplay could be generated from.
/// </summary>
/// <param name="Name">The project name.</param>
/// <param name="IsExecutable">Whether the project produces an executable rather than a library.</param>
public record ScreenplayProjectCandidate(string Name, bool IsExecutable);
