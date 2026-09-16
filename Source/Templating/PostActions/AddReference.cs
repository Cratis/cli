// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Templating.PostActions;

/// <summary>
/// Adds package and project references to generated projects by editing the project XML directly, and
/// resolves <c language="csharp">Version="*"</c> through the CLI's own NuGet client — no toolchain required. A generated
/// project still carrying <c language="csharp">Version="*"</c> is a failed action, never a partial success.
/// </summary>
static class AddReference
{
    public static async Task<PostActionResult> Run(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        Packages.TemplatePackageStore? store,
        string? sourceName,
        CancellationToken cancellationToken)
    {
        var referenceType = action.Args.GetValueOrDefault("referenceType", "package");
        var reference = action.Args.GetValueOrDefault("reference")
            ?? throw new InvalidTemplateManifest($"post action '{action.ActionId}' is missing arg 'reference'.");
        var extensions = (action.Args.GetValueOrDefault("projectFileExtensions") ?? ".csproj")
            .Split(';', StringSplitOptions.RemoveEmptyEntries);

        var allProjects = result.PrimaryOutputs
            .Where(path => extensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .Concat(Directory.EnumerateFiles(result.OutputRoot, "*", SearchOption.AllDirectories)
                .Where(path => extensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var projects = ResolveTargetFiles(action, result, sourceName, allProjects);
        if (projects.Length == 0)
        {
            return new PostActionResult(action, PostActionOutcome.Failed, "no project files found in output.", PostActionRunner.JoinInstructions(action));
        }

        var resolvedVersion = action.Args.GetValueOrDefault("version");
        if (referenceType == "package" && string.IsNullOrEmpty(resolvedVersion))
        {
            resolvedVersion = "*";
        }

        foreach (var project in projects)
        {
            if (referenceType == "package")
            {
                await AddPackageReference(action, project, reference, resolvedVersion!, store, cancellationToken);
            }
            else
            {
                InsertProjectReference(project, reference);
            }
        }

        return new PostActionResult(
            action,
            PostActionOutcome.Succeeded,
            $"added {referenceType} reference '{reference}' to {projects.Length} project(s).",
            string.Empty);
    }

    /// <summary>
    /// Resolves the targetFiles argument to concrete output paths. Entries are globs against the
    /// template's source tree; the instantiated file names carry the name replacement, so the
    /// entry's file name is mapped through sourceName to the instantiated name before matching.
    /// Without targetFiles, every discovered project file is a target.
    /// </summary>
    /// <param name="action">The action whose targetFiles argument is resolved.</param>
    /// <param name="result">The instantiation result naming the output tree.</param>
    /// <param name="sourceName">The manifest sourceName, when set.</param>
    /// <param name="allProjects">Every discovered project file in the output.</param>
    /// <returns>The concrete target project files.</returns>
    static string[] ResolveTargetFiles(
        Configuration.PostActionConfig action,
        InstantiationResult result,
        string? sourceName,
        string[] allProjects)
    {
        var targetFiles = (action.Args.GetValueOrDefault("targetFiles") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (targetFiles.Length == 0)
        {
            return allProjects;
        }

        var targets = new List<string>();
        foreach (var entry in targetFiles)
        {
            var fileName = Path.GetFileName(entry);
            if (sourceName is not null)
            {
                fileName = fileName.Replace(sourceName, result.Name, StringComparison.Ordinal);
            }

            targets.AddRange(allProjects.Where(project =>
                string.Equals(Path.GetFileName(project), fileName, StringComparison.OrdinalIgnoreCase)));
        }
        return [.. targets.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    static async Task AddPackageReference(
        Configuration.PostActionConfig action,
        string project,
        string packageId,
        string version,
        Packages.TemplatePackageStore? store,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(project, cancellationToken);
        var existing = Regex.Match(content, $"<PackageReference\\s+Include=\"{Regex.Escape(packageId)}\"[^>]*/>", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        if (existing.Success && !content.Contains($"Include=\"{packageId}\" Version=\"*\"", StringComparison.Ordinal))
        {
            return;
        }

        var resolved = version;
        if (version == "*" || existing is { Success: true })
        {
            resolved = await ResolveVersion(packageId);
        }

        if (existing.Success)
        {
            // Replace the placeholder element in place, preserving the indentation it already
            // carries — the upstream post action keeps the element where the template put it.
            var replacement = $"<PackageReference Include=\"{packageId}\" Version=\"{resolved}\" />";
            content = content.Replace(existing.Value, replacement, StringComparison.Ordinal);
        }
        else
        {
            content = InsertIntoItemGroup(content, $"    <PackageReference Include=\"{packageId}\" Version=\"{resolved}\" />");
        }
        await File.WriteAllTextAsync(project, content, cancellationToken);

        var final = await File.ReadAllTextAsync(project, cancellationToken);
        if (final.Contains($"Include=\"{packageId}\" Version=\"*\"", StringComparison.Ordinal))
        {
            throw new TemplatePackageAcquisitionError(
                $"project '{project}' still carries Version=\"*\" for '{packageId}' — the scaffold failed to resolve a concrete version.");
        }
        _ = store;
    }

    static async Task<string> ResolveVersion(string packageId)
    {
        foreach (var feed in Packages.NuGetConfig.DiscoverFeeds(Environment.CurrentDirectory))
        {
            try
            {
                if (Packages.NuGetClient.IsLocalFolder(feed))
                {
                    var localVersions = Packages.NuGetClient.GetLocalVersions(feed, packageId);
                    if (localVersions is null || localVersions.Count == 0)
                    {
                        continue;
                    }
                    return localVersions
                        .Select(Version.Parse)
                        .OrderDescending()
                        .First()
                        .ToString();
                }

                var client = new Packages.NuGetClient();
                return await client.GetLatestVersion(feed, packageId);
            }
            catch (TemplatePackageAcquisitionError)
            {
                // Try the next feed.
            }
        }
        throw new TemplatePackageAcquisitionError($"could not resolve a concrete version for package '{packageId}' from any configured feed.");
    }

    static void InsertProjectReference(string project, string reference)
    {
        var content = File.ReadAllText(project);
        if (content.Contains($"Include=\"{reference}\"", StringComparison.Ordinal))
        {
            return;
        }
        content = InsertIntoItemGroup(content, $"    <ProjectReference Include=\"{reference}\" />");
        File.WriteAllText(project, content);
    }

    static string InsertIntoItemGroup(string content, string entry)
    {
        var itemGroupEnd = content.IndexOf("</ItemGroup>", StringComparison.Ordinal);
        if (itemGroupEnd >= 0)
        {
            return content.Insert(itemGroupEnd, $"{entry}\n  ");
        }

        var projectEnd = content.LastIndexOf("</Project>", StringComparison.Ordinal);
        if (projectEnd < 0)
        {
            throw new InvalidTemplateManifest("project file has no ItemGroup and no </Project> to insert into.");
        }
        return content.Insert(projectEnd, $"  <ItemGroup>\n  {entry}\n  </ItemGroup>\n");
    }
}
