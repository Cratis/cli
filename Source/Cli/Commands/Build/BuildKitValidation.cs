// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Validates a kit the way Studio does (version 1 of the manifest format): the manifest against its schema, that every file
/// and folder it names is in the kit and not empty, that every skill folder has a <c language="csharp">SKILL.md</c>, and that every
/// placeholder is one a host fills. Each problem starts with a JSON pointer into the manifest, or <c language="csharp">(document)</c>.
/// </summary>
public static partial class BuildKitValidation
{
    static readonly string[] _members =
        ["kitVersion", "name", "version", "description", "contract", "screenplay", "sliceTypes", "ui", "scaffold", "instructions", "skills", "prompt", "verify", "git", "permissions", "existingCodebase", "requires"];

    static readonly string[] _required = ["kitVersion", "name", "version", "description", "sliceTypes", "instructions", "prompt", "verify"];
    static readonly string[] _sliceTypes = ["stateChange", "stateView", "automation", "translation"];

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex KitName { get; }

    [GeneratedRegex(@"^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SemanticVersion { get; }

    [GeneratedRegex(@"^\./(?!.*(^|/)\.\.(/|$))[^\\]+$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex KitPath { get; }

    [GeneratedRegex(@"^(?!/)(?!.*(^|/)\.\.(/|$))[^\\]+$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ProjectPath { get; }

    /// <summary>
    /// Finds every problem with a kit.
    /// </summary>
    /// <param name="kit">The kit.</param>
    /// <returns>The problems; none when the kit is valid.</returns>
    public static IEnumerable<string> Problems(BuildKit kit)
    {
        var manifest = kit.Manifest;
        foreach (var member in manifest.Select(pair => pair.Key).Where(member => !_members.Contains(member))) yield return $"/{member}: the manifest format does not declare this member.";
        foreach (var member in _required.Where(member => manifest[member] is null)) yield return $"/{member}: the member is required.";

        if (manifest["kitVersion"] is not null && Text(manifest["kitVersion"]) != "1") yield return "/kitVersion: must be \"1\".";
        if (Text(manifest["name"]) is { } name && (name.Length > 64 || !KitName.IsMatch(name))) yield return "/name: must be lowercase words joined by hyphens, at most 64 characters.";
        if (manifest["version"] is not null && (Text(manifest["version"]) is not { } version || !SemanticVersion.IsMatch(version))) yield return "/version: must be a semantic version such as 1.0.0.";
        if (manifest["description"] is not null && Text(manifest["description"]) is not { Length: > 0 and <= 1000 }) yield return "/description: must be text of 1 to 1000 characters.";
        if (manifest["ui"] is { } ui && (ui is not JsonValue value || !value.TryGetValue<bool>(out _))) yield return "/ui: must be true or false.";

        foreach (var problem in SliceTypeProblems(manifest["sliceTypes"])) yield return problem;
        foreach (var problem in KitPathProblems(kit)) yield return problem;
        foreach (var problem in CommandProblems(manifest["verify"], "/verify")) yield return problem;
        foreach (var problem in PlaceholderProblems(kit)) yield return problem;
        foreach (var problem in ProjectPathProblems(manifest)) yield return problem;
    }

    static IEnumerable<string> SliceTypeProblems(JsonNode? node)
    {
        if (node is null) yield break;
        if (node is not JsonArray { Count: > 0 } types)
        {
            yield return "/sliceTypes: must name at least one slice type.";
            yield break;
        }

        for (var index = 0; index < types.Count; index++)
        {
            if (!_sliceTypes.Contains(Text(types[index]))) yield return $"/sliceTypes/{index}: must be one of {string.Join(", ", _sliceTypes)}.";
        }
    }

    static IEnumerable<string> KitPathProblems(BuildKit kit)
    {
        foreach (var (pointer, path, isFolder) in KitPaths(kit))
        {
            if (!KitPath.IsMatch(path))
            {
                yield return $"{pointer}: must start with ./ and never leave the kit.";
                continue;
            }

            var full = kit.PathOf(path);
            if (isFolder ? !Directory.Exists(full) || !Directory.EnumerateFileSystemEntries(full).Any() : !File.Exists(full) || new FileInfo(full).Length == 0)
            {
                yield return $"{pointer}: {path} is not in the kit, or is empty.";
            }
        }

        if (kit.Skills is { } skills && KitPath.IsMatch(skills) && Directory.Exists(kit.PathOf(skills)))
        {
            foreach (var skill in Directory.EnumerateDirectories(kit.PathOf(skills)).Where(folder => !File.Exists(Path.Combine(folder, "SKILL.md"))))
            {
                yield return $"/skills: the skill {Path.GetFileName(skill)} has no SKILL.md.";
            }
        }
    }

    static IEnumerable<(string Pointer, string Path, bool IsFolder)> KitPaths(BuildKit kit)
    {
        if (Text(kit.Manifest["instructions"]) is { } instructions) yield return ("/instructions", instructions, false);
        if (Text(kit.Manifest["prompt"]) is { } prompt) yield return ("/prompt", prompt, false);
        if (kit.Skills is { } skills) yield return ("/skills", skills, true);
        if (kit.ScaffoldPath is { } scaffold) yield return (kit.Manifest["scaffold"] is JsonValue ? "/scaffold" : "/scaffold/path", scaffold, true);
        if (kit.Learn is { } learn) yield return ("/existingCodebase/learn", learn, false);
    }

    static IEnumerable<string> CommandProblems(JsonNode? node, string pointer)
    {
        if (node is null) yield break;
        if (node is not JsonArray { Count: > 0 } commands)
        {
            yield return $"{pointer}: must list at least one command.";
            yield break;
        }

        for (var index = 0; index < commands.Count; index++)
        {
            if (Text(commands[index]) is not { Length: > 0 and <= 1000 }) yield return $"{pointer}/{index}: must be a command of 1 to 1000 characters.";
        }
    }

    static IEnumerable<string> PlaceholderProblems(BuildKit kit)
    {
        var templates = new List<(string Pointer, string Text)>();
        if (Text(kit.Manifest["prompt"]) is { } prompt && KitPath.IsMatch(prompt) && File.Exists(kit.PathOf(prompt))) templates.Add(("/prompt", kit.ReadText(prompt)));
        if (kit.Learn is { } learn && KitPath.IsMatch(learn) && File.Exists(kit.PathOf(learn))) templates.Add(("/existingCodebase/learn", kit.ReadText(learn)));
        templates.AddRange(kit.Verify.Select((command, index) => ($"/verify/{index}", command)));
        if (kit.GitBranch is { } branch) templates.Add(("/git/branch", branch));
        if (kit.GitCommit is { } commit) templates.Add(("/git/commit", commit));

        foreach (var (pointer, text) in templates)
        {
            foreach (var unknown in BuildKitPlaceholders.Unknown(text))
            {
                yield return $"{pointer}: {{{unknown}}} is not a placeholder a host fills; use one of {string.Join(", ", BuildKitPlaceholders.Names.Select(known => $"{{{known}}}"))}.";
            }
        }
    }

    static IEnumerable<string> ProjectPathProblems(JsonObject manifest)
    {
        var paths = (manifest["permissions"]?["paths"] as JsonArray ?? []).Select((path, index) => ($"/permissions/paths/{index}", Text(path))).ToList();
        if (manifest["existingCodebase"] is JsonObject existing)
        {
            paths.Add(("/existingCodebase/conventions", Text(existing["conventions"])));
            if (existing["learn"] is null) yield return "/existingCodebase/learn: the member is required.";
        }

        foreach (var (pointer, path) in paths.Where(entry => entry.Item2 is null || !ProjectPath.IsMatch(entry.Item2)))
        {
            yield return $"{pointer}: must be relative to the project and never leave it.";
        }
    }

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
