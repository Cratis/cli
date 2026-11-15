// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;

namespace Cratis.Templating.Packages;

internal sealed class PackageStorePath(DirectoryInfo root)
{
    internal static StringComparer Comparer { get; } = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal string Root { get; } = ResolveRoot(root);

    internal static void RefuseLinkedRoot(DirectoryInfo directory) => RefuseLink(directory);

    internal static bool IsSafeLocalName(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        return !InvalidPart(name) && name.IndexOfAny(['/', '\\']) < 0;
    }

    internal string Child(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(Root, relative));
        var prefix = Path.TrimEndingDirectorySeparator(Root) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new TemplatePackageAcquisitionError("package path must be within its directory.");
        }
        Check(new FileInfo(path));
        return path;
    }

    internal void Check(FileSystemInfo entry)
    {
        var current = entry.FullName;
        while (current.Length >= Root.Length)
        {
            RefuseLink(new FileInfo(current));
            if (Comparer.Equals(current, Root)) return;
            current = Path.GetDirectoryName(current)!;
        }
        throw new TemplatePackageAcquisitionError("package path must be within its directory.");
    }

    internal void CheckTree(DirectoryInfo directory)
    {
        Check(directory);
        if (!Directory.Exists(directory.FullName)) return;
        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            Check(entry);
            if (entry is DirectoryInfo child) CheckTree(child);
        }
    }

    internal IReadOnlyList<(ZipArchiveEntry Entry, string Target, bool Directory)> Entries(ZipArchive archive)
    {
        var entries = archive.Entries.Select(Plan).ToArray();
        if (entries.Select(entry => entry.Target.Normalize(NormalizationForm.FormC)).Distinct(Comparer).Count() != entries.Length)
        {
            throw new TemplatePackageAcquisitionError("package entries must have distinct destinations.");
        }
        var files = entries.Where(entry => !entry.Directory).Select(entry => entry.Target.Normalize(NormalizationForm.FormC)).ToHashSet(Comparer);
        foreach (var entry in entries)
        {
            CheckFileAncestors(new FileInfo(entry.Target), files);
            if (entry.Directory ? File.Exists(entry.Target) : Directory.Exists(entry.Target))
            {
                throw new TemplatePackageAcquisitionError("package entry does not match its destination kind.");
            }
        }
        return entries;
    }

    static bool InvalidPart(string part) => string.IsNullOrEmpty(part) || part.EndsWith('.') || part.EndsWith(' ')
        || part.StartsWith(".cratis-", StringComparison.OrdinalIgnoreCase)
        || part.Any(character => char.IsControl(character) || character is ':' or '<' or '>' or '"' or '|' or '?' or '*')
        || IsDeviceName(part);

    static bool IsDeviceName(string part)
    {
        var name = part.Split('.')[0];
        return new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    static bool IsLink(ZipArchiveEntry entry)
    {
        var kind = (entry.ExternalAttributes >> 16) & 0xf000;
        return kind is not (0 or 0x8000 or 0x4000) || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0;
    }

    static void RefuseLink(FileSystemInfo entry)
    {
        entry.Refresh();
        if (entry.LinkTarget is not null || IsReparsePoint(entry))
        {
            throw new TemplatePackageAcquisitionError("package directories must not contain links.");
        }
    }

    static bool IsReparsePoint(FileSystemInfo entry) => (File.Exists(entry.FullName) || Directory.Exists(entry.FullName)) && File.GetAttributes(entry.FullName).HasFlag(FileAttributes.ReparsePoint);

    static string ResolveRoot(DirectoryInfo root)
    {
        var full = root.FullName;
        var current = Path.GetPathRoot(full)!;
        foreach (var component in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            var info = new DirectoryInfo(current);
            if (info.LinkTarget is not null || IsReparsePoint(info))
            {
                current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                    ?? throw new TemplatePackageAcquisitionError("package store root could not be resolved.");
            }
        }
        return Path.TrimEndingDirectorySeparator(current);
    }

    void CheckFileAncestors(FileInfo target, HashSet<string> files)
    {
        var parent = Path.GetDirectoryName(target.FullName);
        while (parent is not null && !Comparer.Equals(parent, Root))
        {
            if (files.Contains(parent.Normalize(NormalizationForm.FormC)) || File.Exists(parent))
            {
                throw new TemplatePackageAcquisitionError("package file cannot be used as a directory.");
            }
            parent = Path.GetDirectoryName(parent);
        }
    }

    (ZipArchiveEntry Entry, string Target, bool Directory) Plan(ZipArchiveEntry entry)
    {
        var name = entry.FullName.Replace('\\', '/');
        var directory = name.EndsWith('/');
        var parts = (directory ? name[..^1] : name).Split('/');
        if (parts.Any(InvalidPart) || IsLink(entry))
        {
            throw new TemplatePackageAcquisitionError("invalid package entry.");
        }
        return (entry, Child(Path.Combine(parts)), directory);
    }
}
