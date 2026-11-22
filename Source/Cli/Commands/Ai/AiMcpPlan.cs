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

    AiMcpPlan(string project, bool restoreMissing = false)
    {
        _project = AiProjectPaths.PhysicalRoot(project, requireExists: false);
        _members = new(
            _project,
            removedOwnedIsAbsent: restoreMissing,
            userOwnedRemedy: entry => $"rename or remove your entry, or set mcpServers.{entry.Id}.enabled=false in .cratis/ai.json",
            reportRestored: restoreMissing);
    }

    internal List<AiManagedMcpServer> Installed => _members.Installed;
    internal List<string> Conflicts => _members.Conflicts;
    internal List<string> Unsupported { get; } = [];
    internal List<string> Extensions { get; } = [];
    internal List<string> Actions => _members.Actions;

    internal static AiMcpPlan Create(string project, AiMcpCatalog catalog, AiConfiguration configuration, ISet<string> selectedProfiles, AiInstallationManifest previous, bool hasPiBridge)
    {
        var plan = new AiMcpPlan(project, restoreMissing: true);
        var desired = new List<AiManagedMcpServer>();
        foreach (var settings in (configuration.McpServers ?? new Dictionary<string, AiMcpConfiguration>()).Values)
        {
            if (settings.Root is not null) AiProjectPaths.Within(plan._project, settings.Root);
        }
        if (catalog.Notice is not null) plan.Unsupported.Add(catalog.Notice);
        foreach (var skipped in catalog.Skipped.Where(skipped => skipped.Profiles.Any(selectedProfiles.Contains))) plan.Unsupported.Add($"MCP server '{skipped.Id}' skipped: {skipped.Reason}.");
        foreach (var descriptor in catalog.Servers.Where(descriptor => descriptor.Profiles.Any(selectedProfiles.Contains)))
        {
            desired.AddRange(plan.PlanServer(descriptor, configuration, previous.McpExtensions ?? [], hasPiBridge));
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
        var descriptors = AiMcpDescriptor.Read(AiProjectPaths.PhysicalRoot(project)).Servers;
        return [.. plan.Conflicts, .. (manifest.McpServers ?? [])
            .Where(entry => !AiMcpHarnesses.HasAllowedLaunch(entry, descriptors))
            .Select(entry => $"{entry.Path}:{entry.Collection}.{entry.Id} ({LaunchProblem(entry, descriptors)})")];
    }

    internal static IReadOnlyList<string> RootProblems(string project, AiConfiguration configuration, AiInstallationManifest manifest)
    {
        if ((manifest.McpServers ?? []).Count == 0 && (manifest.McpExtensions ?? []).Count == 0) return [];
        var physical = AiProjectPaths.PhysicalRoot(project);
        var descriptors = AiMcpDescriptor.Read(physical).Servers;
        var problems = new List<string>();
        foreach (var id in (manifest.McpServers ?? []).Select(server => server.Id).Concat((manifest.McpExtensions ?? []).Select(extension => extension.Split('/')[1])).Distinct(StringComparer.Ordinal))
        {
            var problem = RootProblem(physical, id, configuration.McpServers?.GetValueOrDefault(id));
            if (problem is not null) problems.Add(problem);
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

    static string LaunchProblem(AiManagedMcpServer entry, IReadOnlyList<AiMcpDescriptor> descriptors)
    {
        if (entry.Id == "screenplay") return "managed MCP launch differs from 'cratis screenplay mcp'";
        return descriptors.Any(descriptor => descriptor.Id == entry.Id)
            ? "managed MCP launch differs from the corpus descriptor"
            : "managed MCP launch: descriptor no longer present";
    }

    static string? RootProblem(string project, string id, AiMcpConfiguration? settings)
    {
        if (settings?.Enabled == false) return $"MCP {id} is disabled but still registered; run 'cratis ai update'.";
        var root = settings?.Root;
        return root is not null && !Directory.Exists(AiProjectPaths.Within(project, root)) ? $"MCP model directory missing: {root}" : null;
    }

    List<AiManagedMcpServer> PlanServer(AiMcpDescriptor descriptor, AiConfiguration configuration, IReadOnlyList<string> previousExtensions, bool hasPiBridge)
    {
        var desired = new List<AiManagedMcpServer>();
        if (configuration.McpServers?.GetValueOrDefault(descriptor.Id)?.Enabled == false) return desired;
        var root = descriptor.Root(configuration);
        ValidateRoot(root);
        foreach (var harness in configuration.Harnesses.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var entry = PlanHarness(harness, descriptor, previousExtensions, hasPiBridge);
            if (entry is not null) desired.Add(entry);
        }
        if (root is not null && (desired.Count > 0 || Extensions.Contains($"pi/{descriptor.Id}", StringComparer.Ordinal))) _roots.Add(root);
        return desired;
    }

    void ValidateRoot(string? root)
    {
        if (root is null) return;
        var path = AiProjectPaths.Within(_project, root);
        if (File.Exists(path) && !Directory.Exists(path)) throw new AiMcpConfigurationInvalid($"MCP model root is not a directory: {root}");
    }

    AiManagedMcpServer? PlanHarness(string harness, AiMcpDescriptor descriptor, IReadOnlyList<string> previousExtensions, bool hasPiBridge)
    {
        if (string.Equals(harness, "pi", StringComparison.OrdinalIgnoreCase) && hasPiBridge && descriptor.Id == "screenplay")
        {
            Extensions.Add($"pi/{descriptor.Id}");
            if (!previousExtensions.Contains($"pi/{descriptor.Id}", StringComparer.Ordinal)) Actions.Add($"Configured MCP pi/{descriptor.Id} through the native cratis-mcp extension");
            return null;
        }
        var entry = AiMcpHarnesses.Render(_project, harness.ToLowerInvariant(), descriptor);
        if (entry is null) Unsupported.Add(AiMcpHarnesses.Unsupported(harness, descriptor.Id));
        return entry;
    }

    void Prepare(IReadOnlyList<AiManagedMcpServer> desired, IReadOnlyList<AiManagedMcpServer> previous)
    {
        _members.Prepare(desired, previous, AiMcpHarnesses.ValidateOwned);
        foreach (var root in _roots.Where(root => !Directory.Exists(AiProjectPaths.Within(_project, root)))) Actions.Add($"Created MCP model directory {root}");
    }
}
