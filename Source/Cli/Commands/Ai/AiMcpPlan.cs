// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Preflights member-level ownership before any corpus or host configuration is changed.
/// </summary>
internal sealed class AiMcpPlan
{
    readonly string _project;
    readonly Dictionary<string, IAiMcpDocument> _documents = new(StringComparer.Ordinal);
    readonly List<string> _roots = [];

    AiMcpPlan(string project) => _project = AiProjectPaths.PhysicalRoot(project, requireExists: false);

    internal List<AiManagedMcpServer> Installed { get; } = [];
    internal List<string> Conflicts { get; } = [];
    internal List<string> Unsupported { get; } = [];
    internal List<string> Extensions { get; } = [];
    internal List<string> Actions { get; } = [];

    internal static AiMcpPlan Create(string project, IReadOnlyList<AiMcpDescriptor> descriptors, AiConfiguration configuration, ISet<string> selectedProfiles, AiInstallationManifest previous, bool hasPiBridge)
    {
        var plan = new AiMcpPlan(project);
        var desired = new List<AiManagedMcpServer>();
        foreach (var settings in (configuration.McpServers ?? new Dictionary<string, AiMcpConfiguration>()).Values)
        {
            if (settings.Root is not null) AiProjectPaths.Within(plan._project, settings.Root);
        }
        foreach (var descriptor in descriptors.Where(descriptor => descriptor.Profiles.Any(selectedProfiles.Contains)))
        {
            if (configuration.McpServers?.GetValueOrDefault(descriptor.Id)?.Enabled == false) continue;
            var root = descriptor.Root(configuration);
            var rootPath = AiProjectPaths.Within(plan._project, root);
            if (File.Exists(rootPath) && !Directory.Exists(rootPath)) throw new AiMcpConfigurationInvalid($"MCP model root is not a directory: {root}");
            foreach (var harness in configuration.Harnesses.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(harness, "pi", StringComparison.OrdinalIgnoreCase) && hasPiBridge && descriptor.Id == "screenplay")
                {
                    plan.Extensions.Add($"pi/{descriptor.Id}");
                    if (!(previous.McpExtensions ?? []).Contains($"pi/{descriptor.Id}", StringComparer.Ordinal)) plan.Actions.Add($"Configured MCP pi/{descriptor.Id} through the native cratis-mcp extension");
                    continue;
                }
                var entry = AiMcpHarnesses.Render(plan._project, harness.ToLowerInvariant(), descriptor);
                if (entry is null) plan.Unsupported.Add(AiMcpHarnesses.Unsupported(harness, descriptor.Id));
                else desired.Add(entry);
            }
            if (desired.Exists(entry => entry.Id == descriptor.Id) || plan.Extensions.Contains($"pi/{descriptor.Id}", StringComparer.Ordinal)) plan._roots.Add(root);
        }
        plan.Prepare(desired, previous.McpServers ?? []);
        return plan;
    }

    internal static AiMcpPlan Removal(string project, AiInstallationManifest manifest)
    {
        var plan = new AiMcpPlan(project);
        plan.Prepare([], manifest.McpServers ?? []);
        return plan;
    }

    internal static IReadOnlyList<string> Drift(string project, AiInstallationManifest manifest)
    {
        var plan = Removal(project, manifest);
        return plan.Conflicts;
    }

    internal static IReadOnlyList<string> RootProblems(string project, AiConfiguration configuration, AiInstallationManifest manifest)
    {
        if ((manifest.McpServers ?? []).Count == 0 && (manifest.McpExtensions ?? []).Count == 0) return [];
        var physical = AiProjectPaths.PhysicalRoot(project);
        var descriptors = AiMcpDescriptor.Read(physical);
        var problems = new List<string>();
        foreach (var id in (manifest.McpServers ?? []).Select(server => server.Id).Concat((manifest.McpExtensions ?? []).Select(extension => extension.Split('/')[1])).Distinct(StringComparer.Ordinal))
        {
            var settings = configuration.McpServers?.GetValueOrDefault(id);
            if (settings?.Enabled == false)
            {
                problems.Add($"MCP {id} is disabled but still registered; run 'cratis ai update'.");
                continue;
            }
            var root = settings?.Root ?? descriptors.FirstOrDefault(server => server.Id == id)?.DefaultRoot ?? AiMcpDescriptor.ScreenplayRoot;
            if (!Directory.Exists(AiProjectPaths.Within(physical, root))) problems.Add($"MCP model directory missing: {root}");
        }
        return problems;
    }

    internal void Apply(AiFileOperations operations)
    {
        if (Conflicts.Count > 0) throw new AiMcpConfigurationInvalid("Cannot apply an MCP plan with conflicts.");
        foreach (var root in _roots)
        {
            var path = AiProjectPaths.Within(_project, root);
            if (!Directory.Exists(path)) operations.CreateDirectory(path);
        }
        foreach (var document in _documents.Values) document.Apply(operations);
    }

    static bool SameMember(AiManagedMcpServer left, AiManagedMcpServer right) =>
        left.Path == right.Path && left.Collection == right.Collection && left.Id == right.Id;

    void Prepare(IReadOnlyList<AiManagedMcpServer> desired, IReadOnlyList<AiManagedMcpServer> previous)
    {
        foreach (var entry in previous)
        {
            AiMcpHarnesses.ValidateOwned(entry);
            var document = Document(entry.Path);
            if (!JsonNode.DeepEquals(document.Get(entry.Collection, entry.Id), entry.Installed))
            {
                Conflicts.Add($"{entry.Path}:{entry.Collection}.{entry.Id} (modified owned MCP entry)");
                continue;
            }
            if (desired.Any(candidate => SameMember(candidate, entry))) continue;
            document.Set(entry.Collection, entry.Id, entry.Preimage);
            Actions.Add($"Removed MCP {entry.Harness}/{entry.Id} from {entry.Path}");
        }
        foreach (var entry in desired)
        {
            var document = Document(entry.Path);
            var owned = previous.FirstOrDefault(candidate => SameMember(candidate, entry));
            var current = document.Get(entry.Collection, entry.Id);
            if (owned is null && document.Contains(entry.Collection, entry.Id))
            {
                Conflicts.Add($"{entry.Path}:{entry.Collection}.{entry.Id} (user-owned MCP entry)");
                continue;
            }
            if (owned is not null && !JsonNode.DeepEquals(current, owned.Installed)) continue;
            if (!JsonNode.DeepEquals(current, entry.Installed))
            {
                document.Set(entry.Collection, entry.Id, entry.Installed);
                Actions.Add($"Configured MCP {entry.Harness}/{entry.Id} in {entry.Path}");
            }
            Installed.Add(entry);
        }
        foreach (var root in _roots.Where(root => !Directory.Exists(AiProjectPaths.Within(_project, root)))) Actions.Add($"Created MCP model directory {root}");
    }

    IAiMcpDocument Document(string path)
    {
        if (!_documents.TryGetValue(path, out var document))
        {
            document = path.EndsWith(".toml", StringComparison.Ordinal)
                ? new AiMcpTomlDocument(_project, path)
                : new AiMcpDocument(_project, path);
            _documents.Add(path, document);
        }
        return document;
    }
}
