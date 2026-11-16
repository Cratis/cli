// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

sealed record ScreenplayModelDirectory(string Path)
{
    static readonly HashSet<string> _skipped = [with(StringComparer.OrdinalIgnoreCase), "node_modules", "bin", "obj", "artifacts", "dist", "out", "packages"];

    public string Name => System.IO.Path.GetFileName(Path);

    public bool Exists => Directory.Exists(Path);

    public bool IsLink => File.GetAttributes(Path).HasFlag(FileAttributes.ReparsePoint);

    public bool IsScannable => !Name.StartsWith('.') && !_skipped.Contains(Name) && !IsLink;

    public bool HoldsWorkspaceState => ScreenplayStateRoots.Select(Path, Path, null) is not null;

    public bool IsStateFolder => !IsLink && HoldsWorkspaceState;

    public bool ContainsPlayFiles => Directory.EnumerateFiles(Path, "*.play", SearchOption.AllDirectories).Any();

    public IEnumerable<ScreenplayModelDirectory> Directories => Directory.EnumerateDirectories(Path).Select(path => new ScreenplayModelDirectory(path));

    public IEnumerable<string> Entries => Directory.EnumerateFileSystemEntries(Path);
}
