// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Finds the directory holding a project's Screenplay model, relative to the project root.
/// </summary>
/// <remarks>
/// An existing model always wins: the deepest directory containing every .play file. Otherwise the project's
/// Source or src folder is chosen only when it already holds Screenplay workspace state; a source folder
/// without a model or workspace state is application code, not a model root. In every other case a Screenplay
/// folder at the project root is chosen; it is not created until the first write. Nothing under .cratis is ever
/// chosen for new work. A folder between the project and that model which already holds Screenplay workspace
/// state is served instead, so applied identities and interrupted writes are never left behind.
/// </remarks>
internal static class ScreenplayModelLocation
{
    const int MaximumEntries = 50_000;
    const int MaximumDepth = 10;
    const string Fallback = "Screenplay";
    static readonly string[] _sourceFolders = ["Source", "src"];
    static readonly HashSet<string> _skipped = [with(StringComparer.OrdinalIgnoreCase), "node_modules", "bin", "obj", "artifacts", "dist", "out", "packages"];

    internal static string Locate(string project, Action<string>? report = null)
    {
        var discovered = Discover(project);

        // A folder between the project and the model that already holds workspace state keeps being served.
        if (ScreenplayStateRoots.Select(project, discovered, report) is { } stateRoot) return stateRoot;

        // The root is only resolved here: the Screenplay MCP server creates it on the first write, so starting
        // the server in a code-first repository leaves no footprint.
        return discovered;
    }

    static string Discover(string project)
    {
        // A model created by earlier versions lives in .cratis/screenplay; keep serving it where it exists.
        var legacy = Path.Combine(project, ".cratis", "screenplay");
        if (Directory.Exists(legacy) && ContainsPlayFiles(legacy)) return legacy;

        if (Existing(project) is { } existing) return existing;

        return StateSourceFolder(project) ?? Path.Combine(project, Fallback);
    }

    static string? StateSourceFolder(string project)
    {
        foreach (var name in _sourceFolders)
        {
            var candidate = Directory.EnumerateDirectories(project).FirstOrDefault(directory =>
                string.Equals(Path.GetFileName(directory), name, StringComparison.OrdinalIgnoreCase));
            if (IsStateFolder(candidate)) return candidate;
        }

        return null;
    }

    static bool IsStateFolder(string? candidate) => candidate is not null && !IsLink(candidate) && HoldsWorkspaceState(candidate);

    static bool HoldsWorkspaceState(string folder) => ScreenplayStateRoots.Select(folder, folder, null) is not null;

    static string? Existing(string project)
    {
        var directories = new List<string>();
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((project, 0));
        var entries = 0;
        while (pending.TryPop(out var current))
        {
            if (!TryScan(current, pending, ref entries, out var found)) return null;
            if (found) directories.Add(current.Path);
        }

        return directories.Count == 0 ? null : CommonAncestor(directories);
    }

    static bool TryScan((string Path, int Depth) current, Stack<(string Path, int Depth)> pending, ref int entries, out bool found)
    {
        found = false;
        foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
        {
            if (++entries > MaximumEntries) return false;
            if (!Directory.Exists(entry))
            {
                found |= IsPlayFile(entry);
                continue;
            }

            if (ShouldDescend(entry, current.Depth)) pending.Push((entry, current.Depth + 1));
        }

        return true;
    }

    static bool IsPlayFile(string path) => Path.GetFileName(path).EndsWith(".play", StringComparison.OrdinalIgnoreCase);

    static bool ShouldDescend(string directory, int depth) => depth < MaximumDepth && IsScannableName(Path.GetFileName(directory)) && !IsLink(directory);

    static bool IsScannableName(string name) => !name.StartsWith('.') && !_skipped.Contains(name);

    static string CommonAncestor(List<string> directories)
    {
        var common = directories[0].Split(Path.DirectorySeparatorChar);
        foreach (var directory in directories.Skip(1))
        {
            var parts = directory.Split(Path.DirectorySeparatorChar);
            common = common[..CommonPrefixLength(common, parts)];
        }

        return string.Join(Path.DirectorySeparatorChar, common);
    }

    static int CommonPrefixLength(string[] common, string[] parts)
    {
        var limit = Math.Min(common.Length, parts.Length);
        var length = 0;
        while (length < limit && string.Equals(common[length], parts[length], StringComparison.Ordinal)) length++;
        return length;
    }

    static bool ContainsPlayFiles(string directory) => Directory.EnumerateFiles(directory, "*.play", SearchOption.AllDirectories).Any();

    static bool IsLink(string path) => File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
