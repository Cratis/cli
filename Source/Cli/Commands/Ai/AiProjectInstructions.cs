// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Finds a project's own instructions and splits them into the managed project concern files.</summary>
static class AiProjectInstructions
{
    internal const string RelativePath = ".cratis/ai/rules/project.md";

    static readonly string[] _legacyPaths = [".cratis/PROJECT.md", ".ai/PROJECT.md", ".agents/PROJECT.md"];

    internal static string? Find(DirectoryInfo project)
    {
        var destination = Path.Combine(project.FullName, RelativePath);
        if (File.Exists(destination)) return destination;
        return _legacyPaths.Select(candidate => Path.Combine(project.FullName, candidate)).FirstOrDefault(File.Exists);
    }

    internal static Migration? Plan(DirectoryInfo project, string? source, AiFileOperations operations)
    {
        if (source is null) return null;
        var destination = Path.Combine(project.FullName, RelativePath);
        var concernsDirectory = Path.Combine(Path.GetDirectoryName(destination)!, "project");
        if (Directory.Exists(concernsDirectory)) return null;

        operations.Observe("read", source);
        var lines = File.ReadAllText(source).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var headingIndexes = FindHeadings(lines);
        if (headingIndexes.Count == 0) return new(new(source), new(destination), null, []);

        var sections = PlanSections(new DirectoryInfo(concernsDirectory), lines, headingIndexes);
        foreach (var section in sections)
        {
            RequireUsableName(source, section);
            operations.Ownership?.BeforeMutation(section.Path.FullName, "migration preflight");
        }
        var preamble = string.Join('\n', lines[..headingIndexes[0]]).TrimEnd();
        var index = $"{preamble}\n\n## Project concerns\n\nRead every concern below before working in this repository. Together they are the project-owned instructions and override conflicting shared guidance.\n\n{string.Join('\n', sections.Select(section => section.Link))}\n";
        return new(new(source), new(destination), index, sections);
    }

    internal static void Migrate(DirectoryInfo project, Migration? migration, List<string> actions, AiFileOperations operations)
    {
        if (migration is null) return;
        var destination = migration.Destination.FullName;
        operations.CreateDirectoryFor(destination);
        if (migration.Index is null)
        {
            if (!AiCorpusSynchronizer.PathExists(destination)) operations.Copy(migration.Source.FullName, destination);
            return;
        }

        operations.CreateDirectory(Path.Combine(Path.GetDirectoryName(destination)!, "project"));
        foreach (var section in migration.Sections) operations.WriteAllText(section.Path.FullName, section.Content);
        operations.WriteAllText(destination, migration.Index);
        actions.Add($"Split {Path.GetRelativePath(project.FullName, migration.Source.FullName).Replace('\\', '/')} into {RelativePath} and {RelativePath[..^3]}/");
    }

    static void RequireUsableName(string source, Section section)
    {
        try
        {
            AiOwnershipPlan.Canonical($"rules/project/{section.Path.Name}");
        }
        catch (AiMcpConfigurationInvalid)
        {
            throw new AiMcpConfigurationInvalid($"Cannot split {Path.GetFileName(source)} into project concerns: its heading would become '{section.Path.Name}', which is a reserved file name. Rename that heading in {source} and run again.");
        }
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

    static Section[] PlanSections(DirectoryInfo concernsDirectory, string[] lines, List<int> headingIndexes)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var sections = new List<Section>();
        for (var sectionIndex = 0; sectionIndex < headingIndexes.Count; sectionIndex++)
        {
            var start = headingIndexes[sectionIndex];
            var end = sectionIndex + 1 < headingIndexes.Count ? headingIndexes[sectionIndex + 1] : lines.Length;
            var title = lines[start][3..].Trim();
            var name = UniqueName(Slug(title), usedNames);
            var content = string.Join('\n', lines[start..end]).TrimEnd();
            sections.Add(new(new(Path.Combine(concernsDirectory.FullName, $"{name}.md")), $"---\napplyTo: \"**/*\"\n---\n\n{content}\n", $"- [{title}]({RelativePath[..^3]}/{name}.md)"));
        }
        return [.. sections];
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

    internal sealed record Migration(FileInfo Source, FileInfo Destination, string? Index, Section[] Sections);
    internal sealed record Section(FileInfo Path, string Content, string Link);
}
