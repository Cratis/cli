// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Materializes ownership before inspecting destinations. Path checks are repeated at use boundaries;
/// they do not make pathname-based .NET operations atomic against concurrent directory replacement.
/// </summary>
sealed class AiOwnershipPlan
{
    static readonly HashSet<string> _devices = [with(StringComparer.OrdinalIgnoreCase), "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$"];
    static readonly HashSet<string> _traversal = [with(StringComparer.Ordinal), string.Empty, ".", ".."];
    static readonly HashSet<string> _devicePrefixes = [with(StringComparer.OrdinalIgnoreCase), "COM", "LPT"];
    readonly DirectoryInfo _project;
    readonly AiFileOperations _operations;
    readonly HashSet<AiManagedIntegration> _integrations;
    readonly Dictionary<string, AiManagedIntegration> _previous;
    readonly Dictionary<string, string> _files;
    readonly HashSet<string> _pendingDeletes = [with(StringComparer.Ordinal)];

    AiOwnershipPlan(DirectoryInfo project, AiOwnershipSelection selection, AiFileOperations operations)
    {
        var (previous, desired, harnesses) = selection;
        _project = project;
        _operations = operations;
        ValidateManifest(previous);
        ValidateFiles(previous.Files.Select(file => file.Destination));
        ValidateFiles(desired.Select(asset => asset.Destination));
        _files = previous.Files.Select(file => file.Destination).Concat(desired.Select(asset => asset.Destination))
            .Distinct(StringComparer.Ordinal).ToDictionary(destination => destination, destination => Path.Combine(project.FullName, AiCorpusSynchronizer.ManagedRoot, destination), StringComparer.Ordinal);
        var registry = AiHarnessIntegrations.Registry(previous.Files.Select(file => file.Destination));
        ValidateIntegrations(previous.Integrations ?? [], registry);
        var withoutInstructions = AiHarnessIntegrations.Plan(harnesses, desired.Select(asset => asset.Destination), false).Values;
        var withInstructions = AiHarnessIntegrations.Plan(harnesses, desired.Select(asset => asset.Destination), true).Values;
        ValidateIntegrations(withoutInstructions, null);
        ValidateIntegrations(withInstructions, null);
        _integrations = [.. previous.Integrations ?? [], .. withoutInstructions, .. withInstructions];
        _previous = (previous.Integrations ?? []).ToDictionary(integration => integration.Path, StringComparer.Ordinal);
        ValidateFiles(_files.Keys);
    }

    internal static AiOwnershipPlan Create(DirectoryInfo project, AiOwnershipSelection selection, AiFileOperations operations)
    {
        var plan = new AiOwnershipPlan(project, selection, operations);
        plan.Preflight();
        operations.Ownership = plan;
        return plan;
    }

    internal static AiMcpConfigurationInvalid Invalid(string kind = "manifest", string? entry = null) => new($"AI ownership plan is invalid ({kind}: {JsonSerializer.Serialize(entry)}). Restore trusted installation metadata or perform a clean install; --force cannot bypass ownership checks.");

    internal static void Canonical(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw Invalid("path", path);
        if (IsInvalidPath(path)) throw Invalid("path", path);
    }

    internal static void ValidateSources(string corpus, IEnumerable<AiCorpusAsset> assets)
    {
        var root = Path.Combine(corpus, AiCorpusSynchronizer.ManagedRoot);
        foreach (var asset in assets)
        {
            var relative = Path.GetRelativePath(root, asset.Path).Replace(Path.DirectorySeparatorChar, '/');
            Canonical(relative);
            if (!string.Equals(relative, asset.Destination, StringComparison.Ordinal)) throw Invalid("source destination", asset.Destination);
        }
    }

    internal string ReadFile(string destination)
    {
        var path = FilePath(destination);
        _operations.Observe("read", path);
        path = FilePath(destination);
        return File.ReadAllText(path);
    }

    internal string FilePath(string destination)
    {
        if (!_files.TryGetValue(destination, out var path)) throw Invalid("file", destination);
        Route(new FileInfo(path), AiOwnershipPathKind.ManagedFile);
        return path;
    }

    internal string IntegrationPath(AiManagedIntegration integration)
    {
        if (!_integrations.Contains(integration)) throw Invalid("integration", integration.Path);
        var path = Path.Combine(_project.FullName, integration.Path);
        Route(new FileInfo(path), AiOwnershipPathKind.Integration);
        Route(TargetPath(integration));
        return path;
    }

    internal bool Matches(AiManagedIntegration integration)
    {
        var path = IntegrationPath(integration);
        _operations.Observe("link", path);
        var info = new FileInfo(path);
        if (info.LinkTarget is not { } target || !string.Equals(target, integration.Target, StringComparison.Ordinal)) return false;
        if (!File.Exists(TargetPath(integration).FullName) && !Directory.Exists(TargetPath(integration).FullName)) return true;
        return File.GetAttributes(path).HasFlag(FileAttributes.Directory) == integration.IsDirectory;
    }

    internal List<string> UnownedIntegrations() => [.. _previous.Values
        .Where(integration => AiCorpusSynchronizer.PathExists(IntegrationPath(integration)) && !Matches(integration))
        .Select(integration => integration.Path)];

    internal void BeforeMutation(string path, string operation)
    {
        _operations.Observe(operation, path);
        var relative = Path.GetRelativePath(_project.FullName, path).Replace(Path.DirectorySeparatorChar, '/');
        Canonical(relative);
        var entries = _integrations.Where(integration => integration.Path == relative).ToArray();
        Route(new FileInfo(path), entries.Length > 0 ? AiOwnershipPathKind.Integration : AiOwnershipPathKind.Ancillary);
        foreach (var entry in entries) Route(TargetPath(entry));
        if (operation == "delete" && entries.Length > 0) ValidateDeletion(entries[0]);
        if (operation == "link") ValidateCreation(entries.FirstOrDefault() ?? throw Invalid("integration", relative));
    }

    static void RequireNoLinks(FileSystemInfo path)
    {
        if (path.LinkTarget is not null || IsReparsePoint(path)) throw Invalid("linked route", path.FullName);
    }

    static bool IsReparsePoint(FileSystemInfo path) => AiCorpusSynchronizer.PathExists(path.FullName) && File.GetAttributes(path.FullName).HasFlag(FileAttributes.ReparsePoint);

    static bool IsInvalidPath(string path) => !path.IsNormalized() || HasInvalidCharacters(path) || path.Split('/').Any(InvalidComponent);

    static bool HasInvalidCharacters(string path) => path.IndexOfAny(['\\', ':', '<', '>', '"', '|', '?', '*']) >= 0 || path.Any(char.IsControl);

    static bool InvalidComponent(string component)
    {
        if (IsTraversalOrPadded(component)) return true;
        var name = component.Split('.')[0];
        return _devices.Contains(name) || IsNumberedDevice(name);
    }

    static bool IsTraversalOrPadded(string component) => _traversal.Contains(component) || component.Trim() != component || component.EndsWith('.');

    static bool IsNumberedDevice(string name)
    {
        if (name.Length != 4) return false;
        return _devicePrefixes.Contains(name[..3]) && "123456789¹²³".Contains(name[3]);
    }

    static void ValidateManifest(AiInstallationManifest manifest)
    {
        if (manifest is null) throw Invalid();
        if (string.IsNullOrWhiteSpace(manifest.SourceRevision)) throw Invalid("manifest field", nameof(manifest.SourceRevision));
        if (manifest.Files is null) throw Invalid("manifest field", nameof(manifest.Files));
        foreach (var file in manifest.Files)
        {
            if (file is null) throw Invalid("file");
            if (string.IsNullOrWhiteSpace(file.Source) || string.IsNullOrWhiteSpace(file.Hash)) throw Invalid("file", file.Destination);
        }
    }

    static bool IsProjectOwned(string path) =>
        path.Equals("rules/project.md", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("rules/project", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("rules/project/", StringComparison.OrdinalIgnoreCase);

    static void ValidateFiles(IEnumerable<string> files)
    {
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (var file in files)
        {
            Canonical(file);
            if (IsProjectOwned(file)) throw Invalid("project-owned file", file);
            if (!seen.Add(file)) throw Invalid("duplicate file", file);
        }
        ValidateCollisions(seen);
    }

    static void ValidateIntegrations(IEnumerable<AiManagedIntegration> integrations, HashSet<AiManagedIntegration>? registry)
    {
        HashSet<string> paths = [with(StringComparer.Ordinal)];
        foreach (var integration in integrations)
        {
            if (integration is null || string.IsNullOrWhiteSpace(integration.Target)) throw Invalid("integration", integration?.Path);
            Canonical(integration.Path);
            if (!paths.Add(integration.Path) || registry?.Contains(integration) == false) throw Invalid("integration", integration.Path);
        }
        ValidateCollisions(paths);
    }

    static void ValidateCollisions(HashSet<string> paths)
    {
        HashSet<string> portable = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (var path in paths)
        {
            if (!portable.Add(path)) throw Invalid("case-colliding destination", path);
        }
        foreach (var path in paths)
        {
            if (HasContainedParent(path, portable)) throw Invalid("overlapping destination", path);
        }
    }

    static bool HasContainedParent(string path, HashSet<string> portable)
    {
        var parent = path;
        while (parent.LastIndexOf('/') is var index && index >= 0)
        {
            parent = parent[..index];
            if (portable.Contains(parent)) return true;
        }

        return false;
    }

    void Preflight()
    {
        foreach (var file in _files.Keys) FilePath(file);
        foreach (var integration in _integrations) IntegrationPath(integration);
        foreach (var path in new[] { ".cratis/ai.json", ".cratis/ai.manifest.json", AiProjectInstructions.RelativePath, ".cratis/ai/rules/project", ".cratis/PROJECT.md", ".ai/PROJECT.md", ".agents/PROJECT.md" }) Route(new FileInfo(Path.Combine(_project.FullName, path)));
    }

    void Route(FileSystemInfo path, AiOwnershipPathKind kind = AiOwnershipPathKind.Ancillary)
    {
        var relative = Path.GetRelativePath(_project.FullName, path.FullName).Replace(Path.DirectorySeparatorChar, '/');
        Canonical(relative);
        _operations.Observe("metadata", _project.FullName);
        RequireNoLinks(_project);
        var current = _project.FullName;
        var components = relative.Split('/');
        for (var index = 0; index < components.Length; index++)
        {
            current = Path.Combine(current, components[index]);
            _operations.Observe("metadata", current);
            ValidateRouteComponent(new FileInfo(current), index == components.Length - 1 ? kind : AiOwnershipPathKind.Directory);
        }
    }

    void ValidateRouteComponent(FileSystemInfo path, AiOwnershipPathKind kind)
    {
        if (kind == AiOwnershipPathKind.Integration) return;
        RequireNoLinks(path);
        if (kind == AiOwnershipPathKind.Directory && File.Exists(path.FullName)) throw Invalid("directory route", path.FullName);
        if (_files.ContainsValue(path.FullName) && Directory.Exists(path.FullName)) throw Invalid("file", path.FullName);
    }

    void ValidateDeletion(AiManagedIntegration integration)
    {
        if (!AiCorpusSynchronizer.PathExists(IntegrationPath(integration))) return;
        if (!_previous.TryGetValue(integration.Path, out var owned)) throw Invalid("unowned integration", integration.Path);
        if (!Matches(owned)) throw Invalid("changed integration", integration.Path);
        _pendingDeletes.Add(integration.Path);
    }

    void ValidateCreation(AiManagedIntegration integration)
    {
        if (!AiCorpusSynchronizer.PathExists(IntegrationPath(integration))) return;
        if (!CanReplaceInDryRun(integration)) throw Invalid("existing integration", integration.Path);
    }

    bool CanReplaceInDryRun(AiManagedIntegration integration) => _operations.DryRun && _pendingDeletes.Contains(integration.Path) && _previous.TryGetValue(integration.Path, out var owned) && Matches(owned);

    FileInfo TargetPath(AiManagedIntegration integration) => new(Path.GetFullPath(Path.Combine(_project.FullName, Path.GetDirectoryName(integration.Path)!, integration.Target)));
}
