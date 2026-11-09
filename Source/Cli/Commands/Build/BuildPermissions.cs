// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What the host allows an agent to do when it asks: write the paths the kit permits inside the project, run the commands
/// the kit permits and the verification commands, and read the project and the folders the host hands it. Everything else
/// is refused. An agent is never run with its permission checks switched off.
/// </summary>
/// <param name="Project">The project's root folder.</param>
/// <param name="WritablePaths">Globs, relative to the project, the agent may write.</param>
/// <param name="Commands">Command prefixes the agent may run.</param>
/// <param name="ReadableRoots">Folders outside the project the agent may read, such as the guidance.</param>
/// <param name="WritableFiles">Single files outside the project the agent may write, such as its outcome.</param>
public sealed partial record BuildPermissions(
    string Project,
    IReadOnlyList<string> WritablePaths,
    IReadOnlyList<string> Commands,
    IReadOnlyList<string> ReadableRoots,
    IReadOnlyList<string> WritableFiles)
{
    static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    [GeneratedRegex(@"[;&|`<>\n\r]|\$\(", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ShellComposition { get; }

    /// <summary>
    /// Gets a value indicating whether the agent may write a file.
    /// </summary>
    /// <param name="path">The file, absolute or relative to the project.</param>
    /// <returns><see langword="true"/> when it may.</returns>
    public bool AllowsWrite(string path)
    {
        var full = Full(path);
        if (WritableFiles.Any(file => string.Equals(Path.GetFullPath(file), full, _pathComparison))) return true;
        if (RelativeToProject(full) is not { } relative) return false;
        if (relative == ".git" || relative.StartsWith(".git/", StringComparison.Ordinal)) return false;
        return WritablePaths.Any(glob => Glob(glob).IsMatch(relative));
    }

    /// <summary>
    /// Gets a value indicating whether the agent may read a file.
    /// </summary>
    /// <param name="path">The file, absolute or relative to the project.</param>
    /// <returns><see langword="true"/> when it may.</returns>
    public bool AllowsRead(string path)
    {
        var full = Full(path);
        return RelativeToProject(full) is not null || AllowsWrite(full) ||
            ReadableRoots.Any(root => Inside(Path.GetFullPath(root), full));
    }

    /// <summary>
    /// Gets a value indicating whether the agent may run a command. A command that chains, pipes, redirects or substitutes
    /// another is refused whatever it starts with.
    /// </summary>
    /// <param name="command">The command line.</param>
    /// <returns><see langword="true"/> when it may.</returns>
    public bool AllowsCommand(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.Length == 0 || ShellComposition.IsMatch(trimmed)) return false;
        return Commands.Any(prefix =>
        {
            var allowed = prefix.Trim();
            return allowed.Length > 0 && (trimmed == allowed || trimmed.StartsWith(allowed + " ", StringComparison.Ordinal));
        });
    }

    /// <summary>
    /// Converts a glob relative to the project into a regular expression over project-relative paths with forward slashes.
    /// </summary>
    /// <param name="glob">The glob: <c language="csharp">**</c> spans folders, <c language="csharp">*</c> and <c language="csharp">?</c> stay within one.</param>
    /// <returns>The expression.</returns>
    public static Regex Glob(string glob)
    {
        var normalized = glob.Replace('\\', '/');
        normalized = normalized.StartsWith("./", StringComparison.Ordinal) ? normalized[2..] : normalized;
        var pattern = Regex.Escape(normalized)
            .Replace(@"\*\*/", "(.*/)?", StringComparison.Ordinal)
            .Replace(@"\*\*", ".*", StringComparison.Ordinal)
            .Replace(@"\*", "[^/]*", StringComparison.Ordinal)
            .Replace(@"\?", "[^/]", StringComparison.Ordinal);
        return new($"^{pattern}$", OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase : RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    string Full(string path) => Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(Project, path));

    string? RelativeToProject(string full)
    {
        var root = Path.GetFullPath(Project);
        if (!Inside(root, full)) return null;
        var relative = Path.GetRelativePath(root, full).Replace('\\', '/');
        return relative == "." ? string.Empty : relative;
    }

    static bool Inside(string root, string full)
    {
        var withSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return string.Equals(root, full, _pathComparison) || full.StartsWith(withSeparator, _pathComparison);
    }
}
