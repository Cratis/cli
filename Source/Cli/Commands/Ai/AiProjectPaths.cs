// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Resolves the deliberately selected project anchor, then refuses links beneath it.
/// </summary>
internal static class AiProjectPaths
{
    internal static string PhysicalRoot(string path, bool requireExists = true)
    {
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full)!;
        foreach (var segment in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var directory = new DirectoryInfo(Path.Combine(current, segment));
            current = directory.LinkTarget is not null
                ? directory.ResolveLinkTarget(true)?.FullName ?? throw new AiMcpConfigurationInvalid($"Unresolvable project directory: {directory.FullName}")
                : directory.FullName;
        }
        if (requireExists && !Directory.Exists(current)) throw new AiMcpConfigurationInvalid($"Project directory does not exist: {current}");
        return current;
    }

    internal static string Within(string project, string relative)
    {
        ValidateRelative(relative);
        var current = project;
        foreach (var segment in relative.Split('/', StringSplitOptions.RemoveEmptyEntries).Where(segment => segment != "."))
        {
            if (File.Exists(current) && !Directory.Exists(current)) throw new AiMcpConfigurationInvalid($"Expected a project directory, found a file: {current}");
            current = Path.Combine(current, segment);
            if (new FileInfo(current).LinkTarget is not null || new DirectoryInfo(current).LinkTarget is not null)
            {
                throw new AiMcpConfigurationInvalid($"Refusing symbolic link in project configuration or model root: {current}");
            }
        }
        return current;
    }

    internal static void ValidateRelative(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) ||
            relative.IndexOfAny(['\\', ':', '$', '{', '}', '\0', '\r', '\n']) >= 0 ||
            relative.Split('/').Contains("..", StringComparer.Ordinal))
        {
            throw new AiMcpConfigurationInvalid($"MCP root must be a portable project-relative directory without '..', links, or placeholders: '{relative}'.");
        }
    }
}
