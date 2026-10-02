// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Represents the project a view was asked for, or why none could be found.
/// </summary>
/// <param name="ProjectFile">The full path of the project file, or <see langword="null"/> when none was found.</param>
/// <param name="Error">Why no project file was found, or <see langword="null"/> when one was.</param>
public record ViewTarget(string? ProjectFile, string? Error)
{
    /// <summary>
    /// Resolves the project file a path names.
    /// </summary>
    /// <param name="path">A project file, or a folder holding exactly one.</param>
    /// <returns>The resolved <see cref="ViewTarget"/>.</returns>
    /// <remarks>
    /// A folder is not searched recursively: <c language="shell">cratis view</c> is run where the project is, and picking one of
    /// several projects beneath the folder would be a guess about which application was meant.
    /// </remarks>
    public static ViewTarget Resolve(string path)
    {
        var full = System.IO.Path.GetFullPath(path);

        if (File.Exists(full))
        {
            return string.Equals(System.IO.Path.GetExtension(full), ".csproj", StringComparison.OrdinalIgnoreCase)
                ? new(full, null)
                : new(null, $"'{full}' is not a C# project file (.csproj)");
        }

        if (!Directory.Exists(full))
        {
            return new(null, $"'{full}' does not exist");
        }

        var projects = Directory.GetFiles(full, "*.csproj", SearchOption.TopDirectoryOnly);
        return projects.Length switch
        {
            1 => new(projects[0], null),
            0 => new(null, $"'{full}' holds no C# project file (.csproj)"),
            _ => new(null, $"'{full}' holds {projects.Length} C# project files; name the one to view")
        };
    }
}
