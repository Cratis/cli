// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Ai.Testing;

internal static class AiTestPaths
{
    internal static void Validate(string relative)
    {
        var message = $"Path or glob must stay relative to the workspace without '..': {relative}";
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidOperationException(message);
        if (relative.Contains(':', StringComparison.Ordinal) || relative.Contains('\\', StringComparison.Ordinal)) throw new InvalidOperationException(message);
        if (relative.Split('/').Contains("..", StringComparer.Ordinal)) throw new InvalidOperationException(message);
    }

    internal static string Within(string root, string relative)
    {
        Validate(relative);
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var current = root;
        foreach (var segment in relative.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.GetFullPath(Path.Combine(current, segment));
            var info = new FileInfo(current);
            if (info.LinkTarget is not null) current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? throw new InvalidOperationException($"Broken link: {relative}");
            if (!current.StartsWith(fullRoot, StringComparison.Ordinal)) throw new InvalidOperationException($"Path escapes workspace: {relative}");
        }
        return current;
    }

    internal static string[] Select(AiTestCheck check, string workspace)
    {
        var selection = check.Path ?? check.Glob!;
        Validate(selection);
        if (check.Path is not null)
        {
            if (Excluded(check.Path, check.Phase, selection)) return [];
            var path = Within(workspace, check.Path);
            return File.Exists(path) ? [path] : [];
        }
        var pattern = "\\A" + Regex.Escape(selection).Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal).Replace(@"\*\*", ".*", StringComparison.Ordinal).Replace(@"\*", "[^/]*", StringComparison.Ordinal).Replace(@"\?", "[^/]", StringComparison.Ordinal) + "\\z";
        var regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return [.. Files(workspace, workspace, check.Phase, selection).Where(path => regex.IsMatch(Path.GetRelativePath(workspace, path).Replace('\\', '/'))).Select(path => Within(workspace, Path.GetRelativePath(workspace, path).Replace('\\', '/')))];
    }

    static bool Excluded(string relative, AiTestPhase phase, string selection)
    {
        var parts = relative.Split('/');
        if (phase == AiTestPhase.Result) return parts.Intersect([".git", ".cratis", "node_modules"], StringComparer.Ordinal).Any();
        return parts.Contains(".cratis", StringComparer.Ordinal) && !selection.StartsWith(".cratis/", StringComparison.Ordinal);
    }

    static IEnumerable<string> Files(string directory, string root, AiTestPhase phase, string selection)
    {
        foreach (var file in Directory.GetFiles(directory).Where(path => !Excluded(Path.GetRelativePath(root, path).Replace('\\', '/'), phase, selection))) yield return file;
        foreach (var child in Directory.GetDirectories(directory).Where(path => !Excluded(Path.GetRelativePath(root, path).Replace('\\', '/'), phase, selection) && !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)))
        {
            foreach (var file in Files(child, root, phase, selection)) yield return file;
        }
    }
}
