// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cratis.Templating.PostActions;

/// <summary>
/// Adds projects to .sln and .slnx solutions by writing the solution files directly, honoring
/// solutionFolder nesting — no dotnet sln involved.
/// </summary>
static class AddToSolution
{
    public static Task<PostActionResult> Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        var solutionFolder = action.Args.GetValueOrDefault("solutionFolder");
        var solutionPath = FindOrCreateSolutionPath(action, result);
        var projects = result.PrimaryOutputs
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (projects.Length == 0)
        {
            return Task.FromResult(new PostActionResult(
                action, PostActionOutcome.Failed, "no project files among primary outputs.", PostActionRunner.JoinInstructions(action)));
        }

        foreach (var project in projects)
        {
            if (solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                AddToSlnx(solutionPath, project, solutionFolder);
            }
            else
            {
                AddToSln(solutionPath, project, solutionFolder);
            }
        }

        return Task.FromResult(new PostActionResult(
            action,
            PostActionOutcome.Succeeded,
            $"added {projects.Length} project(s) to '{Path.GetFileName(solutionPath)}'.",
            string.Empty));
    }

    static string FindOrCreateSolutionPath(Configuration.PostActionConfig action, InstantiationResult result)
    {
        var configured = action.Args.GetValueOrDefault("solutionFile");
        if (configured is not null)
        {
            return Path.IsPathRooted(configured) ? configured : Path.Combine(result.OutputRoot, configured);
        }

        var existing = Directory.EnumerateFiles(result.OutputRoot, "*.slnx", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(result.OutputRoot, "*.sln", SearchOption.AllDirectories))
            .FirstOrDefault();
        return existing ?? Path.Combine(result.OutputRoot, $"{result.Name}.sln");
    }

    static void AddToSlnx(string solutionPath, string project, string? solutionFolder)
    {
        if (!File.Exists(solutionPath))
        {
            File.WriteAllText(solutionPath, "<Solution>\n</Solution>\n");
        }

        var content = File.ReadAllText(solutionPath);
        var relative = Path.GetRelativePath(Path.GetDirectoryName(solutionPath)!, project).Replace('\\', '/');
        if (content.Contains(relative, StringComparison.Ordinal))
        {
            return;
        }

        var folderAttribute = solutionFolder is null ? string.Empty : $" Folder=\"{solutionFolder}\"";
        var insertion = $"  <Project Path=\"{relative}\"{folderAttribute} />\n";
        var solutionEnd = content.LastIndexOf("</Solution>", StringComparison.Ordinal);
        content = solutionEnd >= 0
            ? content.Insert(solutionEnd, insertion)
            : content + insertion;
        File.WriteAllText(solutionPath, content);
    }

    static void AddToSln(string solutionPath, string project, string? solutionFolder)
    {
        var content = File.Exists(solutionPath) ? File.ReadAllText(solutionPath) : string.Empty;
        var relative = Path.GetRelativePath(Path.GetDirectoryName(solutionPath)!, project).Replace('\\', '/');
        if (content.Contains(relative, StringComparison.Ordinal))
        {
            return;
        }

        var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
        var projectType = Guid.Parse("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC").ToString("B").ToUpperInvariant();

        var folderGuid = Guid.Empty;
        if (solutionFolder is not null && !content.Contains($"\"{solutionFolder}\"", StringComparison.Ordinal))
        {
            folderGuid = Guid.NewGuid();
            var folderType = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8").ToString("B").ToUpperInvariant();
            content += $"Project(\"{folderType}\") = \"{solutionFolder}\", \"{solutionFolder}\", \"{folderGuid:B}\"\nEndProject\n";
        }

        content += $"Project(\"{projectType}\") = \"{Path.GetFileNameWithoutExtension(project)}\", \"{relative}\", \"{projectGuid}\"\nEndProject\n";
        content += "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\n";
        content += $"\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\n\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\n\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU\n\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU\n\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU\n\tEndGlobalSection\n";
        if (folderGuid != Guid.Empty)
        {
            content += $"\tGlobalSection(NestedProjects) = preSolution\n\t\t{projectGuid} = {folderGuid:B}\n\tEndGlobalSection\n";
        }
        content += "EndGlobal\n";
        File.WriteAllText(solutionPath, content);
    }
}

/// <summary>
/// Changes Unix file permissions on a primary output; reported as a no-op on Windows.
/// </summary>
static class ChangePermissions
{
    public static PostActionResult Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        if (OperatingSystem.IsWindows())
        {
            return new PostActionResult(action, PostActionOutcome.NotPerformed, "not applicable on Windows.", string.Empty);
        }

        var mode = action.Args.GetValueOrDefault("chmod")
            ?? throw new InvalidTemplateManifest($"post action '{action.ActionId}' is missing arg 'chmod'.");
        string? target;
        if (action.Args.GetValueOrDefault("fileName") is { } fileName)
        {
            target = ResolveTarget(fileName, result);
        }
        else
        {
            target = result.PrimaryOutputs.Count > 0 ? result.PrimaryOutputs[0] : null;
        }
        if (target is null || !File.Exists(target))
        {
            return new PostActionResult(action, PostActionOutcome.Failed, $"file '{target}' was not found.", PostActionRunner.JoinInstructions(action));
        }

        var permissions = ParseMode(mode);
        File.SetUnixFileMode(target, permissions);
        return new PostActionResult(action, PostActionOutcome.Succeeded, $"set {mode} on '{target}'.", string.Empty);
    }

    static string? ResolveTarget(string fileName, InstantiationResult result) =>
        int.TryParse(fileName, out var index) && index < result.PrimaryOutputs.Count
            ? result.PrimaryOutputs[index]
            : Path.Combine(result.OutputRoot, fileName);

    static UnixFileMode ParseMode(string mode)
    {
        if (mode.Equals("+x", StringComparison.Ordinal))
        {
            return UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute
                | UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        }

        if (mode.Length == 3 && mode.All(char.IsDigit))
        {
            var value = Convert.ToInt32(mode, 8);
            return (UnixFileMode)value;
        }

        throw new InvalidTemplateManifest($"chmod value '{mode}' is not a three-digit octal mode or '+x'.");
    }
}

/// <summary>
/// Adds a property to a JSON file using node editing, preserving the rest of the document.
/// </summary>
static class AddJsonProperty
{
    public static async Task<PostActionResult> Run(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        Packages.TemplatePackageStore? store,
        CancellationToken cancellationToken)
    {
        var file = action.Args.GetValueOrDefault("jsonFile") ?? action.Args.GetValueOrDefault("file");
        var propertyPath = action.Args.GetValueOrDefault("propertyPath") ?? action.Args.GetValueOrDefault("path");
        var value = action.Args.GetValueOrDefault("value");
        if (file is null || propertyPath is null || value is null)
        {
            return new PostActionResult(
                action, PostActionOutcome.Failed, "args jsonFile/file, propertyPath/path and value are all required.", PostActionRunner.JoinInstructions(action));
        }

        var path = int.TryParse(file, out var index) && index < result.PrimaryOutputs.Count
            ? result.PrimaryOutputs[index]
            : Path.Combine(result.OutputRoot, file);
        if (!File.Exists(path))
        {
            return new PostActionResult(action, PostActionOutcome.Failed, $"json file '{path}' was not found.", PostActionRunner.JoinInstructions(action));
        }

        var node = JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken)) as JsonObject
            ?? throw new InvalidTemplateManifest($"json file '{path}' does not hold an object.");
        var segments = propertyPath.Split('.');
        var current = node;
        for (var segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
        {
            current = current.TryGetPropertyValue(segments[segmentIndex], out var child)
                ? child as JsonObject ?? throw new InvalidTemplateManifest($"segment '{segments[segmentIndex]}' in '{propertyPath}' is not an object.")
                : (JsonObject)(current[segments[segmentIndex]] = new JsonObject());
        }
        current[segments[^1]] = JsonValue.Create(value);
        await File.WriteAllTextAsync(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        _ = store;
        return new PostActionResult(action, PostActionOutcome.Succeeded, $"set '{propertyPath}' in '{Path.GetFileName(path)}'.", string.Empty);
    }
}

/// <summary>
/// Displays manual instructions, selecting by condition.
/// </summary>
static class DisplayInstructions
{
    public static PostActionResult Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        _ = result;
        var instructions = PostActionRunner.JoinInstructions(action);
        return new PostActionResult(
            action,
            PostActionOutcome.Displayed,
            action.Description ?? "manual instructions",
            instructions);
    }
}

/// <summary>
/// Reports the path of a file to open — a CLI does not launch editors.
/// </summary>
static class OpenInEditor
{
    public static PostActionResult Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        string? target;
        if (action.Args.GetValueOrDefault("fileName") is { } fileName)
        {
            if (int.TryParse(fileName, out var index) && index < result.PrimaryOutputs.Count)
            {
                target = result.PrimaryOutputs[index];
            }
            else
            {
                target = Path.Combine(result.OutputRoot, fileName);
            }
        }
        else
        {
            target = result.PrimaryOutputs.Count > 0 ? result.PrimaryOutputs[0] : null;
        }
        return new PostActionResult(
            action,
            PostActionOutcome.Succeeded,
            $"open '{target}' in your editor.",
            string.Empty);
    }
}
