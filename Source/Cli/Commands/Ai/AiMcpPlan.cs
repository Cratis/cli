// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Preflights member-level ownership before any corpus or host configuration is changed.
/// </summary>
internal sealed class AiMcpPlan
{
    readonly string _project;
    readonly AiMcpMembers _members;
    readonly List<string> _roots = [];

    AiMcpPlan(string project)
    {
        _project = AiProjectPaths.PhysicalRoot(project, requireExists: false);
        _members = new(_project);
    }

    internal List<AiManagedMcpServer> Installed => _members.Installed;
    internal List<string> Conflicts => _members.Conflicts;
    internal List<string> Unsupported { get; } = [];
    internal List<string> Extensions { get; } = [];
    internal List<string> Actions => _members.Actions;

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
        return [.. plan.Conflicts, .. (manifest.McpServers ?? [])
            .Where(entry => !AiMcpHarnesses.HasAllowedLaunch(entry))
            .Select(entry => $"{entry.Path}:{entry.Collection}.{entry.Id} (managed MCP launch differs from 'cratis screenplay mcp')")];
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
        _members.Apply(operations);
    }

    void Prepare(IReadOnlyList<AiManagedMcpServer> desired, IReadOnlyList<AiManagedMcpServer> previous)
    {
        _members.Prepare(desired, previous, AiMcpHarnesses.ValidateOwned);
        foreach (var root in _roots.Where(root => !Directory.Exists(AiProjectPaths.Within(_project, root)))) Actions.Add($"Created MCP model directory {root}");
    }
}
