// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Finds a project's own instructions and splits them into the managed project concern files.</summary>
static class AiProjectInstructions
{
    internal const string RelativePath = ".cratis/ai/rules/project.md";

    static readonly string[] _legacyPaths = [".cratis/PROJECT.md", ".ai/PROJECT.md", ".agents/PROJECT.md"];

    internal static string? Find(string projectPath)
    {
        var destination = Path.Combine(projectPath, RelativePath);
        if (File.Exists(destination)) return destination;
        return _legacyPaths.Select(candidate => Path.Combine(projectPath, candidate)).FirstOrDefault(File.Exists);
    }

    internal static void Migrate(string projectPath, string source, List<string> actions, AiFileOperations operations)
    {
        var destination = Path.Combine(projectPath, RelativePath);
        var concernsDirectory = Path.Combine(Path.GetDirectoryName(destination)!, "project");
        if (Directory.Exists(concernsDirectory)) return;

        var content = File.ReadAllText(source).Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = content.Split('\n');
        var headingIndexes = FindHeadings(lines);

        operations.CreateDirectoryFor(destination);
        if (headingIndexes.Count == 0)
        {
            if (!AiCorpusSynchronizer.PathExists(destination)) operations.Copy(source, destination);
            return;
        }

        operations.CreateDirectory(concernsDirectory);
        var links = WriteSections(concernsDirectory, lines, headingIndexes, operations);
        var preamble = string.Join('\n', lines[..headingIndexes[0]]).TrimEnd();
        var indexContent = $"{preamble}\n\n## Project concerns\n\nRead every concern below before working in this repository. Together they are the project-owned instructions and override conflicting shared guidance.\n\n{string.Join('\n', links)}\n";
        operations.WriteAllText(destination, indexContent);
        actions.Add($"Split {Path.GetRelativePath(projectPath, source).Replace('\\', '/')} into {RelativePath} and {RelativePath[..^3]}/");
    }

    static List<int> FindHeadings(string[] lines)
    {
        var headingIndexes = new List<int>();
        var insideCodeFence = false;
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal)) insideCodeFence = !insideCodeFence;
            else if (!insideCodeFence && lines[index].StartsWith("## ", StringComparison.Ordinal)) headingIndexes.Add(index);
        }
        return headingIndexes;
    }

    static List<string> WriteSections(string concernsDirectory, string[] lines, List<int> headingIndexes, AiFileOperations operations)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var links = new List<string>();
        for (var sectionIndex = 0; sectionIndex < headingIndexes.Count; sectionIndex++)
        {
            var start = headingIndexes[sectionIndex];
            var end = sectionIndex + 1 < headingIndexes.Count ? headingIndexes[sectionIndex + 1] : lines.Length;
            var title = lines[start][3..].Trim();
            var name = UniqueName(Slug(title), usedNames);
            var section = string.Join('\n', lines[start..end]).TrimEnd();
            operations.WriteAllText(Path.Combine(concernsDirectory, $"{name}.md"), $"---\napplyTo: \"**/*\"\n---\n\n{section}\n");
            links.Add($"- [{title}]({RelativePath[..^3]}/{name}.md)");
        }
        return links;
    }

    static string UniqueName(string baseName, HashSet<string> usedNames)
    {
        var name = baseName;
        var suffix = 2;
        while (!usedNames.Add(name)) name = $"{baseName}-{suffix++}";
        return name;
    }

    static string Slug(string value)
    {
        var slug = new StringBuilder();
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character)) slug.Append(character);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        return slug.ToString().Trim('-') is { Length: > 0 } result ? result : "instructions";
    }
}
