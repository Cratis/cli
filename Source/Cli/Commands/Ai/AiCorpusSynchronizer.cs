// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Synchronizes only the files and harness integrations recorded as Cratis-owned in a project.</summary>
public static class AiCorpusSynchronizer
{
    internal const string ManagedRoot = ".cratis/ai";
    const string ConfigurationPath = ".cratis/ai.json";
    const string ManifestPath = ".cratis/ai.manifest.json";
    static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    /// <summary>Installs or updates the selected corpus, preserving changed and unknown files.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <param name="configuration">The requested harnesses, profiles, and languages.</param>
    /// <param name="force">Whether modified managed files may be replaced or removed.</param>
    /// <param name="dryRun">Whether to report the changes instead of making them.</param>
    /// <returns>The changes made, or that would be made, and files requiring user attention.</returns>
    public static SyncResult Synchronize(string projectPath, string corpusPath, AiConfiguration configuration, bool force = false, bool dryRun = false) => Synchronize(projectPath, corpusPath, configuration, new(force, new(dryRun)));

    /// <summary>Reads the selections offered by a Cratis AI corpus.</summary>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <returns>The harnesses, profiles, and languages published by the corpus manifest.</returns>
    public static AiConfiguration Available(string corpusPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpusPath, ManagedRoot, "manifest.json")));
        return new(ReadArray(manifest, "harnesses"), ReadArray(manifest, "profiles"), ReadArray(manifest, "languages"));
    }

    /// <summary>Returns configured and locally modified managed files without changing the project.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <returns>The current installation state.</returns>
    public static AiStatus Status(string projectPath) => Status(projectPath, new(false));

    /// <summary>Removes only unchanged managed files and Cratis-created harness integrations.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <param name="force">Whether modified managed files may be removed.</param>
    /// <param name="dryRun">Whether to report the removals instead of making them.</param>
    /// <returns>The changes made, or that would be made, and files requiring user attention.</returns>
    public static SyncResult Uninstall(string projectPath, bool force = false, bool dryRun = false) => Uninstall(projectPath, force, dryRun, new(dryRun));

    /// <summary>Gets the immutable Git revision for a corpus checkout, or its last-write timestamp when it is not a Git checkout.</summary>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <returns>The source revision identity.</returns>
    public static string Revision(string corpusPath)
    {
        using var process = Process.Start(new ProcessStartInfo("git", $"-C \"{corpusPath}\" rev-parse HEAD")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        });
        if (process is not null)
        {
            var revision = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode == 0 && revision.Length > 0) return revision;
        }
        return Directory.GetLastWriteTimeUtc(corpusPath).ToString("O");
    }

    internal static SyncResult Synchronize(string projectPath, string corpusPath, AiConfiguration configuration, AiCorpusExecution execution)
    {
        var (force, operations) = execution;
        var dryRun = operations.DryRun;
        projectPath = AiProjectPaths.PhysicalRoot(projectPath, requireExists: false);
        var previous = ReadManifest(projectPath);
        corpusPath = AiProjectPaths.PhysicalRoot(corpusPath);
        var assets = AiCorpusResolver.Resolve(corpusPath, configuration).ToArray();
        AiOwnershipPlan.ValidateSources(corpusPath, assets);
        var ownership = AiOwnershipPlan.Create(new DirectoryInfo(projectPath), new(previous, assets, configuration.Harnesses), operations);
        operations.Observe("configuration", projectPath);
        configuration = WithExistingMcpServers(projectPath, configuration);
        AiProjectPaths.Within(projectPath, AiMcpDescriptor.RelativePath);
        var desired = assets.ToDictionary(asset => asset.Destination, StringComparer.Ordinal);
        operations.Observe("mcp", projectPath);
        var mcp = AiMcpPlan.Create(projectPath, AiMcpDescriptor.Read(AiProjectPaths.PhysicalRoot(corpusPath)), configuration, AiCorpusProfiles.Selected(corpusPath, configuration), previous, HasPiBridge(desired));
        if (mcp.Conflicts.Count > 0) return new([], mcp.Conflicts, mcp.Unsupported);
        operations.Observe("instructions", projectPath);
        var projectInstructionsSource = AiProjectInstructions.Find(projectPath);
        var integrationPlans = AiHarnessIntegrations.Plan(configuration.Harnesses, desired.Keys, projectInstructionsSource is not null);
        var installer = new AiCorpusInstaller(ownership, previous, desired, integrationPlans, operations);
        var modified = installer.ModifiedPaths();

        if (installer.UnknownCollisions() is { Count: > 0 } unknownCollisions) return new([], DistinctOrdered(unknownCollisions), mcp.Unsupported);
        if (modified.Count > 0 && !force) return new([], DistinctOrdered(modified), mcp.Unsupported);

        if (!dryRun && installer.UnchangeableModes() is { Count: > 0 } unchangeableModes) return new([], [.. unchangeableModes.Order(StringComparer.Ordinal)], mcp.Unsupported);

        if (projectInstructionsSource is not null) AiProjectInstructions.Migrate(projectPath, projectInstructionsSource, installer.Actions, operations);

        var (installed, installedIntegrations) = installer.Apply();
        mcp.Apply(operations);
        installer.Actions.AddRange(mcp.Actions);
        WriteConfiguration(projectPath, configuration, operations);
        var manifest = new AiInstallationManifest(
            Revision(corpusPath),
            [.. installed.OrderBy(file => file.Destination, StringComparer.Ordinal)],
            [.. installedIntegrations.OrderBy(integration => integration.Path, StringComparer.Ordinal)],
            mcp.Installed,
            mcp.Unsupported,
            mcp.Extensions);
        WriteJson(AiProjectPaths.Within(projectPath, ManifestPath), manifest, operations);
        return new(installer.Actions, [], mcp.Unsupported, installer.Warnings);
    }

    internal static AiStatus Status(string projectPath, AiFileOperations operations)
    {
        projectPath = AiProjectPaths.PhysicalRoot(projectPath, requireExists: false);
        var manifest = ReadManifest(projectPath);
        var ownership = AiOwnershipPlan.Create(new DirectoryInfo(projectPath), new(manifest, [], []), operations);
        operations.Observe("configuration", projectPath);
        var configuration = ReadConfiguration(projectPath);
        var modified = ModifiedFiles(ownership, manifest);
        modified.AddRange(ModifiedIntegrations(ownership, manifest));
        operations.Observe("mcp", projectPath);
        modified.AddRange(AiMcpPlan.Drift(projectPath, manifest));
        modified.AddRange(AiMcpPlan.RootProblems(projectPath, configuration, manifest));
        return new(configuration, manifest.SourceRevision, DistinctOrdered(modified), manifest.UnsupportedMcpServers ?? [], manifest.McpServers ?? [], manifest.McpExtensions ?? []);
    }

    internal static SyncResult Uninstall(string projectPath, bool force, bool dryRun, AiFileOperations operations)
    {
        projectPath = AiProjectPaths.PhysicalRoot(projectPath, requireExists: false);
        var manifest = ReadManifest(projectPath);
        var ownership = AiOwnershipPlan.Create(new DirectoryInfo(projectPath), new(manifest, [], []), operations);
        if (ownership.UnownedIntegrations() is { Count: > 0 } unowned) return new([], DistinctOrdered(unowned));
        operations.Observe("mcp", projectPath);
        var mcp = AiMcpPlan.Removal(projectPath, manifest);
        if (mcp.Conflicts.Count > 0) return new([], mcp.Conflicts);
        var modified = ModifiedFiles(ownership, manifest);
        modified.AddRange(ModifiedIntegrations(ownership, manifest));
        if (modified.Count > 0 && !force) return new([], DistinctOrdered(modified));

        var actions = new List<string>();
        RemoveManagedFiles(ownership, manifest, operations, actions);
        RemoveManagedIntegrations(ownership, manifest, operations, actions);

        mcp.Apply(operations);
        actions.AddRange(mcp.Actions);
        var manifestPath = AiProjectPaths.Within(projectPath, ManifestPath);
        if (File.Exists(manifestPath)) operations.DeleteFile(manifestPath);
        return new(actions, []);
    }

    internal static AiConfiguration ReadConfiguration(string projectPath)
    {
        var path = AiProjectPaths.Within(AiProjectPaths.PhysicalRoot(projectPath), ConfigurationPath);
        if (!File.Exists(path)) throw new InvalidOperationException("No .cratis/ai.json exists. Run 'cratis ai install' first.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return new(ReadArray(document, "harnesses"), ReadArray(document, "profiles"), ReadArray(document, "languages"), AiMcpConfiguration.Read(document.RootElement));
    }

    internal static List<string> ModifiedFiles(AiOwnershipPlan ownership, AiInstallationManifest manifest) => [.. manifest.Files.Where(file =>
    {
        var path = ownership.FilePath(file.Destination);
        return !File.Exists(path) || !string.Equals(Hash(ownership.ReadFile(file.Destination)), file.Hash, StringComparison.Ordinal);
    }).Select(file => file.Destination)];

    internal static List<string> ModifiedIntegrations(AiOwnershipPlan ownership, AiInstallationManifest manifest) => [.. (manifest.Integrations ?? [])
        .Where(integration => !ownership.Matches(integration))
        .Select(integration => integration.Path)];

    internal static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    internal static bool PathExists(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    static bool HasPiBridge(Dictionary<string, AiCorpusAsset> desired) =>
        desired.ContainsKey("harnesses/pi/extensions/cratis-mcp.ts") || desired.ContainsKey("harnesses/pi/extensions/cratis-mcp/index.ts");

    static string[] DistinctOrdered(IEnumerable<string> values) => [.. values.Distinct(StringComparer.Ordinal).Order()];

    static AiConfiguration WithExistingMcpServers(string projectPath, AiConfiguration configuration)
    {
        if (!File.Exists(AiProjectPaths.Within(projectPath, ConfigurationPath))) return configuration;
        var existing = ReadConfiguration(projectPath);
        return configuration with { McpServers = configuration.McpServers ?? existing.McpServers };
    }

    static void RemoveManagedFiles(AiOwnershipPlan ownership, AiInstallationManifest manifest, AiFileOperations operations, List<string> actions)
    {
        foreach (var file in manifest.Files)
        {
            var path = ownership.FilePath(file.Destination);
            if (!File.Exists(path)) continue;
            operations.DeleteFile(path);
            actions.Add($"Removed {file.Destination}");
        }
    }

    static void RemoveManagedIntegrations(AiOwnershipPlan ownership, AiInstallationManifest manifest, AiFileOperations operations, List<string> actions)
    {
        foreach (var integration in manifest.Integrations ?? [])
        {
            if (!PathExists(ownership.IntegrationPath(integration))) continue;
            AiHarnessIntegrations.Delete(ownership, integration, operations);
            actions.Add($"Removed {integration.Path}");
        }
    }

    /// <summary>
    /// Reads an optional array of strings. A configuration may legitimately omit a dimension it does not
    /// constrain, and an absent property must read as "unconstrained" rather than throwing: the property
    /// accessor reports a missing key as a dictionary lookup failure, which says nothing about the file.
    /// </summary>
    /// <param name="document">The parsed configuration or manifest document.</param>
    /// <param name="name">The property to read.</param>
    /// <returns>The values, or an empty array when the property is absent.</returns>
    static string[] ReadArray(JsonDocument document, string name) =>
        document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Select(item => item.GetString()!)]
            : [];

    static AiInstallationManifest ReadManifest(string projectPath)
    {
        var path = AiProjectPaths.Within(AiProjectPaths.PhysicalRoot(projectPath, requireExists: false), ManifestPath);
        if (!File.Exists(path)) return new("unknown", [], []);
        try
        {
            return JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(path)) ?? throw AiOwnershipPlan.Invalid("manifest", ManifestPath);
        }
        catch (JsonException)
        {
            throw AiOwnershipPlan.Invalid("manifest", ManifestPath);
        }
    }

    static void WriteConfiguration(string projectPath, AiConfiguration configuration, AiFileOperations operations)
    {
        var path = AiProjectPaths.Within(projectPath, ConfigurationPath);
        var document = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))!.AsObject() : [];
        document["schemaVersion"] = AiConfiguration.SchemaVersion;
        document["harnesses"] = JsonSerializer.SerializeToNode(configuration.Harnesses);
        document["profiles"] = JsonSerializer.SerializeToNode(configuration.Profiles);
        document["languages"] = JsonSerializer.SerializeToNode(configuration.Languages);
        if (configuration.McpServers is not null) document["mcpServers"] = AiMcpConfiguration.ToJson(configuration.McpServers);
        operations.CreateDirectoryFor(path);
        operations.WriteAllTextAtomically(path, document.ToJsonString(_serializerOptions));
    }

    static void WriteJson(string path, object value, AiFileOperations operations)
    {
        operations.CreateDirectoryFor(path);
        operations.WriteAllTextAtomically(path, JsonSerializer.Serialize(value, _serializerOptions));
    }
}

/// <summary>The result of an install, update, or uninstall operation.</summary>
/// <param name="Actions">The files added, changed, or removed.</param>
/// <param name="Conflicts">Managed files that were changed locally or user-owned paths that would be overwritten.</param>
/// <param name="UnsupportedMcpServers">Selected servers without a supported adapter.</param>
/// <param name="Warnings">Completed changes that need attention, such as scripts that could not be made executable.</param>
public sealed record SyncResult(IReadOnlyList<string> Actions, IReadOnlyList<string> Conflicts, IReadOnlyList<string>? UnsupportedMcpServers = null, IReadOnlyList<string>? Warnings = null);

/// <summary>Read-only installation state.</summary>
/// <param name="Configuration">The configured selection.</param>
/// <param name="SourceRevision">The corpus revision used to synchronize.</param>
/// <param name="ModifiedFiles">Managed files or integrations changed or removed locally.</param>
/// <param name="UnsupportedMcpServers">Selected servers without a supported adapter.</param>
/// <param name="McpServers">Installed native server members.</param>
/// <param name="McpExtensions">Native extensions providing MCP servers.</param>
public sealed record AiStatus(AiConfiguration Configuration, string SourceRevision, IReadOnlyList<string> ModifiedFiles, IReadOnlyList<string>? UnsupportedMcpServers = null, IReadOnlyList<AiManagedMcpServer>? McpServers = null, IReadOnlyList<string>? McpExtensions = null);
