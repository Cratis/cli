// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Writes a kit's instructions and skills where an agent reads them. The instructions go between the lines
/// <c language="csharp">&lt;!-- cratis-build-kit:name --&gt;</c> and <c language="csharp">&lt;!-- /cratis-build-kit:name --&gt;</c>,
/// followed by one line per skill; everything outside that block in an existing file is kept, and installing again
/// replaces only the block.
/// </summary>
public static class BuildKitInstructions
{
    /// <summary>
    /// Gets the line that opens a kit's block.
    /// </summary>
    /// <param name="kit">The kit's name.</param>
    /// <returns>The line.</returns>
    public static string Start(string kit) => $"<!-- cratis-build-kit:{kit} -->";

    /// <summary>
    /// Gets the line that closes a kit's block.
    /// </summary>
    /// <param name="kit">The kit's name.</param>
    /// <returns>The line.</returns>
    public static string End(string kit) => $"<!-- /cratis-build-kit:{kit} -->";

    /// <summary>
    /// Installs a kit's instructions and skills into a project.
    /// </summary>
    /// <param name="kit">The kit.</param>
    /// <param name="project">The project folder.</param>
    /// <param name="form">Where the agent reads instructions and skills.</param>
    /// <param name="log">Where to say what was written.</param>
    /// <returns>The project paths written.</returns>
    public static IReadOnlyList<string> Install(BuildKit kit, string project, BuildAgentForm form, Action<string> log)
    {
        var written = new List<string> { form.Instructions };
        var skills = new List<(string Path, string Description)>();
        if (kit.Skills is { } skillsFolder && Directory.Exists(kit.PathOf(skillsFolder)))
        {
            foreach (var skill in Directory.EnumerateDirectories(kit.PathOf(skillsFolder)).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(skill);
                var folder = $"{form.Skills}/{name}";
                BuildFiles.CopyFolder(skill, Path.Combine(project, folder));
                written.Add($"{folder}/");
                skills.Add(($"{folder}/SKILL.md", DescriptionOf(Path.Combine(skill, "SKILL.md"))));
            }

            log($"Installed the kit's skills into {form.Skills}.");
        }

        var block = Block(kit.Name, kit.ReadText(kit.Instructions), skills);
        var target = Path.Combine(project, form.Instructions);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, Merge(File.Exists(target) ? File.ReadAllText(target) : string.Empty, kit.Name, block));
        log($"Wrote the kit's instructions into {form.Instructions}.");
        return written;
    }

    /// <summary>
    /// Puts a kit's block into the content of an instruction file, replacing the kit's earlier block and keeping the rest.
    /// </summary>
    /// <param name="existing">The file's content, empty for a new file.</param>
    /// <param name="kit">The kit's name.</param>
    /// <param name="block">The block, from its opening to its closing line.</param>
    /// <returns>The new content.</returns>
    public static string Merge(string existing, string kit, string block)
    {
        var start = existing.IndexOf(Start(kit), StringComparison.Ordinal);
        var end = start < 0 ? -1 : existing.IndexOf(End(kit), start, StringComparison.Ordinal);
        if (start >= 0 && end > start) return string.Concat(existing.AsSpan(0, start), block.TrimEnd('\n'), existing.AsSpan(end + End(kit).Length));
        if (existing.Trim().Length == 0) return block;
        return $"{existing.TrimEnd()}\n\n{block}";
    }

    static string Block(string kit, string instructions, List<(string Path, string Description)> skills)
    {
        var text = new StringBuilder()
            .Append(Start(kit)).Append('\n')
            .Append(instructions.Replace("\r\n", "\n", StringComparison.Ordinal).Trim()).Append('\n');
        if (skills.Count > 0)
        {
            text.Append('\n');
            foreach (var (path, description) in skills)
            {
                text.Append("- `").Append(path).Append('`');
                if (description.Length > 0) text.Append(": ").Append(description);
                text.Append('\n');
            }
        }

        text.Append(End(kit)).Append('\n');
        return text.ToString();
    }

    static string DescriptionOf(string skillFile)
    {
        if (!File.Exists(skillFile)) return string.Empty;
        var lines = File.ReadAllLines(skillFile);
        if (lines.Length == 0 || lines[0].Trim() != "---") return string.Empty;
        foreach (var line in lines.Skip(1).TakeWhile(line => line.Trim() != "---"))
        {
            if (line.StartsWith("description:", StringComparison.Ordinal)) return line["description:".Length..].Trim().Trim('"', '\'');
        }

        return string.Empty;
    }
}
