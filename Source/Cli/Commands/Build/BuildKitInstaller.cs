// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Installs a validated kit into a project: scaffolds an empty project, or prepares an existing codebase when the kit can
/// learn one; copies the kit to <c language="csharp">.cratis/build/kit</c>; writes the kit's instructions and skills where
/// the chosen agent reads them; and records the installation in <c language="csharp">.cratis/build/kit.json</c>.
/// </summary>
/// <param name="log">Where the installer says what it did.</param>
public sealed class BuildKitInstaller(Action<string> log)
{
    static readonly string[] _ignoredInEmptyProject = [".git", ".cratis", ".gitignore", ".DS_Store", "README.md", "LICENSE"];

    /// <summary>
    /// Installs a kit.
    /// </summary>
    /// <param name="fetched">The kit, with where it came from.</param>
    /// <param name="project">The project folder.</param>
    /// <param name="agent">The agent whose form to install, or <see langword="null"/> for the form most agents read.</param>
    /// <param name="existing">Whether to treat the project as an existing codebase even when it is empty.</param>
    /// <param name="cancellationToken">Cancels the scaffold commands.</param>
    /// <returns>The installation's record.</returns>
    /// <exception cref="InvalidBuildKit">The kit cannot be installed into the project.</exception>
    public async Task<BuildProjectSettings> Install(BuildKitFetched fetched, string project, string? agent, bool existing, CancellationToken cancellationToken)
    {
        var kit = fetched.Kit;
        Directory.CreateDirectory(project);
        var unmet = await BuildRequirements.Unmet(kit, project, cancellationToken);
        if (unmet.Count > 0) throw new InvalidBuildKit([.. unmet.Select(problem => $"/requires: {problem}")]);

        var occupied = Directory.EnumerateFileSystemEntries(project).Select(Path.GetFileName).Where(name => name is not null && !_ignoredInEmptyProject.Contains(name)).ToList();
        var previous = BuildProjectSettings.Read(project);
        var files = new List<string>(previous?.Kit == kit.Name ? previous.Files : []);
        if (previous?.Kit == kit.Name)
        {
            log($"The kit {kit.Name} is already installed; updating it.");
        }
        else if (existing || occupied.Count > 0)
        {
            if (kit.Learn is null)
            {
                throw new InvalidBuildKit([$"/existingCodebase: the project is not empty ({string.Join(", ", occupied.Take(5))}) and the kit {kit.Name} only builds new projects."]);
            }

            log($"The first build learns this project's conventions into {kit.Conventions}; later builds reuse them.");
        }
        else
        {
            files.AddRange(await Scaffold(kit, project, cancellationToken));
        }

        var installed = Path.Combine(project, BuildProjectSettings.KitFolder);
        if (Directory.Exists(installed)) Directory.Delete(installed, recursive: true);
        BuildFiles.CopyFolder(kit.Root, installed);
        if (kit.ScaffoldPath is { } scaffold && Directory.Exists(Path.Combine(installed, scaffold))) Directory.Delete(Path.Combine(installed, scaffold), recursive: true);
        if (Directory.Exists(Path.Combine(installed, ".git"))) Directory.Delete(Path.Combine(installed, ".git"), recursive: true);
        files.Add($"{BuildProjectSettings.KitFolder}/");
        log($"Installed the kit {kit.Name} {kit.Version} into {BuildProjectSettings.KitFolder}.");

        files.AddRange(BuildKitInstructions.Install(kit, project, BuildAgentForm.For(agent), log));
        files.Add(BuildProjectSettings.File);
        var record = new BuildProjectSettings(kit.Name, kit.Version, fetched.Sha256, fetched.Source, fetched.Commit, agent, [.. files.Distinct(StringComparer.Ordinal)]);
        record.Write(project);
        return record;
    }

    async Task<IReadOnlyList<string>> Scaffold(BuildKit kit, string project, CancellationToken cancellationToken)
    {
        var files = new List<string>();
        if (kit.ScaffoldPath is null && kit.ScaffoldCommands.Count == 0) return files;

        if (kit.ScaffoldPath is { } scaffold)
        {
            var copied = BuildFiles.CopyFolder(kit.PathOf(scaffold), project, overwrite: false);
            files.AddRange(copied.Select(file => file.Replace('\\', '/')));
            log($"Scaffolded {copied.Count} files from the kit.");
        }

        foreach (var command in kit.ScaffoldCommands)
        {
            log($"Running {command}");
            var result = await BuildProcess.Shell(command, project, null, log, cancellationToken);
            if (result.ExitCode != 0) throw new InvalidBuildKit([$"/scaffold/commands: `{command}` exited with {result.ExitCode}."]);
        }

        var marker = Path.Combine(project, BuildConventions.ScaffoldedMarker);
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        await File.WriteAllTextAsync(marker, $"{kit.Name} {kit.Version}{Environment.NewLine}", cancellationToken);
        files.Add(BuildConventions.ScaffoldedMarker);
        return files;
    }
}
