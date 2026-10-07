// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Finds the directory holding a project's Screenplay model, relative to the project root.
/// </summary>
/// <remarks>
/// An existing model always wins: the deepest directory containing every .play file. Otherwise the model
/// belongs under the project's Source or src folder, and when there is none a Screenplay folder
/// is created at the project root. Nothing under .cratis is ever chosen for new work. A folder between the
/// project and that model which already holds Screenplay workspace state is served instead, so applied
/// identities and interrupted writes are never left behind.
/// </remarks>
internal static class ScreenplayModelLocation
{
    const int MaximumEntries = 50_000;
    const int MaximumDepth = 10;
    const string Fallback = "Screenplay";
    static readonly string[] _sourceFolders = ["Source", "src"];
    static readonly HashSet<string> _skipped = new(StringComparer.OrdinalIgnoreCase) { "node_modules", "bin", "obj", "artifacts", "dist", "out", "packages" };

    internal static string Locate(string project, Action<string>? report = null)
    {
        var discovered = Discover(project);

        // A folder between the project and the model that already holds workspace state keeps being served.
        if (ScreenplayStateRoots.Select(project, discovered, report) is { } stateRoot) return stateRoot;

        Directory.CreateDirectory(discovered);
        return discovered;
    }

    static string Discover(string project)
    {
        // A model created by earlier versions lives in .cratis/screenplay; keep serving it where it exists.
        var legacy = Path.Combine(project, ".cratis", "screenplay");
        if (Directory.Exists(legacy) && ContainsPlayFiles(legacy)) return legacy;

        if (Existing(project) is { } existing) return existing;

        foreach (var name in _sourceFolders)
        {
            var candidate = Directory.EnumerateDirectories(project).FirstOrDefault(directory =>
                string.Equals(Path.GetFileName(directory), name, StringComparison.OrdinalIgnoreCase));
            if (candidate is not null && !IsLink(candidate)) return candidate;
        }

        return Path.Combine(project, Fallback);
    }

    static string? Existing(string project)
    {
        var directories = new List<string>();
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((project, 0));
        var entries = 0;
        while (pending.TryPop(out var current))
        {
            var found = false;
            foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
            {
                if (++entries > MaximumEntries) return null;
                var name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                {
                    if (current.Depth < MaximumDepth && !name.StartsWith('.') && !_skipped.Contains(name) && !IsLink(entry)) pending.Push((entry, current.Depth + 1));
                }
                else if (name.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }

            if (found) directories.Add(current.Path);
        }

        return directories.Count == 0 ? null : CommonAncestor(directories);
    }

    static string CommonAncestor(List<string> directories)
    {
        var common = directories[0].Split(Path.DirectorySeparatorChar);
        foreach (var directory in directories.Skip(1))
        {
            var parts = directory.Split(Path.DirectorySeparatorChar);
            var length = 0;
            while (length < common.Length && length < parts.Length && string.Equals(common[length], parts[length], StringComparison.Ordinal)) length++;
            common = common[..length];
        }

        return string.Join(Path.DirectorySeparatorChar, common);
    }

    static bool ContainsPlayFiles(string directory) => Directory.EnumerateFiles(directory, "*.play", SearchOption.AllDirectories).Any();

    static bool IsLink(string path) => File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
