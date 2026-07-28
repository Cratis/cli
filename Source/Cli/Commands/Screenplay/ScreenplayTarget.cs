// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of resolving the path a Screenplay command works on — the solution or project one is
/// generated from, or the document one is validated in.
/// </summary>
/// <param name="Path">The full path that was resolved; <see langword="null"/> when resolution failed.</param>
/// <param name="Error">The reason resolution failed; <see langword="null"/> when it succeeded.</param>
/// <param name="Suggestion">A hint for resolving the failure; <see langword="null"/> when there is none.</param>
public record ScreenplayTarget(string? Path, string? Error, string? Suggestion)
{
    /// <summary>
    /// Gets a value indicating whether a solution or project was resolved.
    /// </summary>
    public bool IsResolved => Path is not null;

    /// <summary>
    /// Gets a resolved target.
    /// </summary>
    /// <param name="path">The full path of the resolved solution or project file.</param>
    /// <returns>The resolved <see cref="ScreenplayTarget"/>.</returns>
    public static ScreenplayTarget Resolved(string path) => new(path, null, null);

    /// <summary>
    /// Gets an unresolved target carrying the reason and a hint.
    /// </summary>
    /// <param name="error">The reason resolution failed.</param>
    /// <param name="suggestion">A hint for resolving the failure.</param>
    /// <returns>The unresolved <see cref="ScreenplayTarget"/>.</returns>
    public static ScreenplayTarget Unresolved(string error, string suggestion) => new(null, error, suggestion);
}
