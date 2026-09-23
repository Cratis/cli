// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cratis.Templating.FileSystem;

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
        return existing ?? Path.Combine(result.OutputRoot, $"{result.Name}.slnx");
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
/// Changes Unix file permissions on primary outputs, using the documented argument shape: the
/// permission expression (e.g. <c language="csharp">+x</c>) as the argument name and a glob or
/// list of file names as its value. Reported as a no-op on Windows.
/// </summary>
static class ChangePermissions
{
    public static PostActionResult Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        if (OperatingSystem.IsWindows())
        {
            return new PostActionResult(action, PostActionOutcome.NotPerformed, "not applicable on Windows.", string.Empty);
        }

        var entry = action.Args.FirstOrDefault(pair => pair.Key.StartsWith('+') || pair.Key.StartsWith('-'));
        if (entry.Key is null)
        {
            return new PostActionResult(
                action,
                PostActionOutcome.Failed,
                "no permission argument found — expected e.g. { \"+x\": \"*.sh\" }.",
                PostActionRunner.JoinInstructions(action));
        }

        var targets = ResolveTargets(entry.Value, result);
        if (targets.Count == 0)
        {
            return new PostActionResult(action, PostActionOutcome.Failed, "no matching files found.", PostActionRunner.JoinInstructions(action));
        }

        foreach (var target in targets)
        {
            var mode = File.GetUnixFileMode(target);
            mode = entry.Key switch
            {
                "+x" => mode | System.IO.UnixFileMode.UserExecute | System.IO.UnixFileMode.GroupExecute | System.IO.UnixFileMode.OtherExecute,
                "-x" => mode & ~(System.IO.UnixFileMode.UserExecute | System.IO.UnixFileMode.GroupExecute | System.IO.UnixFileMode.OtherExecute),
                _ => throw new InvalidTemplateManifest($"permission '{entry.Key}' is not supported — expected +x or -x.")
            };
            File.SetUnixFileMode(target, mode);
        }
        return new PostActionResult(action, PostActionOutcome.Succeeded, $"{entry.Key} on {targets.Count} file(s).", string.Empty);
    }

    static List<string> ResolveTargets(string pattern, InstantiationResult result)
    {
        var candidates = result.PrimaryOutputs.Concat(Directory.EnumerateFiles(result.OutputRoot, "*", SearchOption.AllDirectories)).Distinct(StringComparer.Ordinal);
        return [.. candidates.Where(candidate => GlobMatcher.Matches(Path.GetRelativePath(result.OutputRoot, candidate).Replace('\\', '/'), pattern))];
    }
}

/// <summary>
/// Adds a property to a JSON file using node editing, per the documented arguments:
/// <c language="csharp">jsonFileName</c>, optional <c language="csharp">parentPropertyPath</c> (colon-separated),
/// <c language="csharp">newJsonPropertyName</c> and <c language="csharp">newJsonPropertyValue</c> (valid JSON).
/// </summary>
static class AddJsonProperty
{
    public static async Task<PostActionResult> Run(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        Packages.TemplatePackageStore? store,
        CancellationToken cancellationToken)
    {
        var file = action.Args.GetValueOrDefault("jsonFileName");
        var propertyName = action.Args.GetValueOrDefault("newJsonPropertyName");
        var propertyValue = action.Args.GetValueOrDefault("newJsonPropertyValue");
        if (file is null || propertyName is null || propertyValue is null)
        {
            return new PostActionResult(
                action,
                PostActionOutcome.Failed,
                "args jsonFileName, newJsonPropertyName and newJsonPropertyValue are all required.",
                PostActionRunner.JoinInstructions(action));
        }

        var path = Path.Combine(result.OutputRoot, file);
        if (!File.Exists(path))
        {
            return new PostActionResult(action, PostActionOutcome.Failed, $"json file '{file}' was not found.", PostActionRunner.JoinInstructions(action));
        }

        var node = JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken)) as JsonObject
            ?? throw new InvalidTemplateManifest($"json file '{file}' does not hold an object.");

        // parentPropertyPath uses colons as separators per the documented contract.
        var current = node;
        if (action.Args.TryGetValue("parentPropertyPath", out var parentPath) && !string.IsNullOrEmpty(parentPath))
        {
            foreach (var segment in parentPath.Split(':'))
            {
                current = current.TryGetPropertyValue(segment, out var child)
                    ? child as JsonObject ?? throw new InvalidTemplateManifest($"segment '{segment}' in '{parentPath}' is not an object.")
                    : (JsonObject)(current[segment] = new JsonObject());
            }
        }
        current[propertyName] = JsonNode.Parse(propertyValue)
            ?? throw new InvalidTemplateManifest($"newJsonPropertyValue '{propertyValue}' is not valid JSON.");
        await File.WriteAllTextAsync(path, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        _ = store;
        return new PostActionResult(action, PostActionOutcome.Succeeded, $"set '{propertyName}' in '{file}'.", string.Empty);
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
/// Reports the path of a file to open — a CLI does not launch editors. The documented
/// <c language="csharp">files</c> argument holds indexes into the primary outputs.
/// </summary>
static class OpenInEditor
{
    public static PostActionResult Run(Configuration.PostActionConfig action, InstantiationResult result)
    {
        var indexes = (action.Args.GetValueOrDefault("files") ?? "0").Split(';', StringSplitOptions.RemoveEmptyEntries);
        var targets = new List<string>();
        foreach (var index in indexes)
        {
            if (int.TryParse(index.Trim(), out var position) && position >= 0 && position < result.PrimaryOutputs.Count)
            {
                targets.Add(result.PrimaryOutputs[position]);
            }
        }
        return new PostActionResult(
            action,
            PostActionOutcome.Succeeded,
            targets.Count > 0 ? $"open {string.Join(", ", targets)} in your editor." : "nothing to open.",
            string.Empty);
    }
}
