// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Applies a resolved corpus selection to a project and reports what it replaced, added, or removed.</summary>
/// <param name="projectPath">The physical consuming repository root.</param>
/// <param name="previous">The manifest of the previous installation.</param>
/// <param name="desired">The corpus assets to install, by destination.</param>
/// <param name="integrations">The planned harness integrations, by path.</param>
/// <param name="operations">The file operations to apply, or only report in a dry run.</param>
sealed class AiCorpusInstaller(
    string projectPath,
    AiInstallationManifest previous,
    Dictionary<string, AiCorpusAsset> desired,
    Dictionary<string, AiManagedIntegration> integrations,
    AiFileOperations operations)
{
    readonly Dictionary<string, AiManagedIntegration> _previousIntegrations = (previous.Integrations ?? []).ToDictionary(integration => integration.Path, StringComparer.Ordinal);

    internal List<string> Actions { get; } = [];

    internal List<string> Warnings { get; } = [];

    internal List<string> ModifiedPaths()
    {
        var modified = AiCorpusSynchronizer.ModifiedFiles(projectPath, previous);
        modified.AddRange(AiCorpusSynchronizer.ModifiedIntegrations(projectPath, previous));
        return modified;
    }

    internal List<string> UnknownCollisions()
    {
        var collisions = desired.Keys
            .Where(destination => AiCorpusSynchronizer.PathExists(ManagedPath(destination)) && !previous.Files.Any(file => file.Destination == destination))
            .ToList();
        collisions.AddRange(integrations.Values
            .Where(plan => !plan.PreserveExisting && AiCorpusSynchronizer.PathExists(Path.Combine(projectPath, plan.Path)) && !_previousIntegrations.ContainsKey(plan.Path) && !AiHarnessIntegrations.Matches(projectPath, plan))
            .Select(plan => plan.Path));
        return collisions;
    }

    internal List<string> UnchangeableModes() => [.. desired.Values
        .Select(asset => (asset.Destination, Path: ManagedPath(asset.Destination), Content: MarkedContent(asset), Source: asset.Path))
        .Where(file => AiCorpusFileModes.Change(file.Path, file.Content, file.Source) is { } mode && AiCorpusFileModes.Adds(file.Path, mode) && !AiCorpusFileModes.CanChange(file.Path))
        .Select(file => $"{file.Destination} (it must become executable, but its file mode cannot be changed here; another user may own it, or the file system may refuse mode changes)")];

    internal (List<AiManagedFile> Files, List<AiManagedIntegration> Integrations) Apply()
    {
        var files = InstallAssets();
        RemoveStaleFiles();
        RemoveStaleIntegrations();
        return (files, InstallIntegrations());
    }

    static string MarkedContent(AiCorpusAsset asset) => AiCorpusMarker.Add(asset.Source, asset.Path, File.ReadAllText(asset.Path));

    string ManagedPath(string destination) => Path.Combine(projectPath, AiCorpusSynchronizer.ManagedRoot, destination);

    List<AiManagedFile> InstallAssets()
    {
        var installed = new List<AiManagedFile>();
        foreach (var asset in desired.Values.OrderBy(asset => asset.Destination, StringComparer.Ordinal))
        {
            var destination = ManagedPath(asset.Destination);
            var content = MarkedContent(asset);
            var hash = AiCorpusSynchronizer.Hash(content);
            var existed = File.Exists(destination);
            operations.CreateDirectoryFor(destination);
            var executable = operations.WriteCorpusFile(destination, content, asset.Path);
            installed.Add(new(asset.Source, asset.Destination, hash));
            Actions.Add(existed ? $"Updated {asset.Destination}" : $"Added {asset.Destination}");
            if (!executable) Warnings.Add($"Could not make {asset.Destination} executable: the file system did not apply the mode change. Run it through its interpreter, or move the project to a file system that keeps file modes.");
        }
        return installed;
    }

    void RemoveStaleFiles()
    {
        foreach (var existing in previous.Files.Where(file => !desired.ContainsKey(file.Destination)))
        {
            var destination = ManagedPath(existing.Destination);
            if (!File.Exists(destination)) continue;
            operations.DeleteFile(destination);
            Actions.Add($"Removed {existing.Destination}");
        }
    }

    void RemoveStaleIntegrations()
    {
        foreach (var existing in _previousIntegrations.Values.Where(integration => !integrations.ContainsKey(integration.Path)))
        {
            AiHarnessIntegrations.Delete(projectPath, existing, operations);
            Actions.Add($"Removed {existing.Path}");
        }
    }

    List<AiManagedIntegration> InstallIntegrations()
    {
        var installed = new List<AiManagedIntegration>();
        foreach (var plan in integrations.Values.OrderBy(plan => plan.Path, StringComparer.Ordinal))
        {
            if (IsUserOwned(plan)) continue;
            if (!AiHarnessIntegrations.Matches(projectPath, plan)) Reconfigure(plan);
            installed.Add(plan);
        }
        return installed;
    }

    bool IsUserOwned(AiManagedIntegration plan)
    {
        if (_previousIntegrations.ContainsKey(plan.Path)) return false;
        return (plan.PreserveExisting && AiCorpusSynchronizer.PathExists(Path.Combine(projectPath, plan.Path))) || AiHarnessIntegrations.Matches(projectPath, plan);
    }

    void Reconfigure(AiManagedIntegration plan)
    {
        if (AiCorpusSynchronizer.PathExists(Path.Combine(projectPath, plan.Path))) AiHarnessIntegrations.Delete(projectPath, plan, operations);
        AiHarnessIntegrations.Create(projectPath, plan, operations);
        Actions.Add($"Configured {plan.Path}");
    }
}
