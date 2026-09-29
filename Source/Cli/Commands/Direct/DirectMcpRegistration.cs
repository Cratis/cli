// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Ai;
using Tomlyn;
using Tomlyn.Model;

namespace Cratis.Cli.Commands.Direct;

/// <summary>A change a registration makes, with the exact member value written or removed.</summary>
/// <param name="Client">The client.</param>
/// <param name="Path">The configuration file, with the home directory shown as '~'.</param>
/// <param name="Member">The member path, such as 'mcpServers.cratis-direct'.</param>
/// <param name="Action">'add', 'update' or 'remove'.</param>
/// <param name="Value">The member as it will be written, or as it is being removed, in the file's own format.</param>
internal sealed record DirectMcpChange(string Client, string Path, string Member, string Action, string Value);

/// <summary>The registration state of one client.</summary>
/// <param name="Client">The client.</param>
/// <param name="Path">The configuration file, with the home directory shown as '~'.</param>
/// <param name="State">'registered', 'modified', 'user-owned', 'absent' or 'unsupported'.</param>
/// <param name="Detail">Why the client is unsupported, or what the registration launches.</param>
internal sealed record DirectMcpClientStatus(string Client, string? Path, string State, string? Detail);

/// <summary>
/// Plans registering, removing or inspecting the Direct stdio bridge in AI client configuration, owning only the
/// members it wrote, as recorded in a per-scope manifest.
/// </summary>
internal sealed class DirectMcpRegistration
{
    readonly DirectMcpScope _scope;
    readonly DirectMcpLocations _locations;
    readonly string _root;
    readonly AiMcpMembers _members;
    readonly DirectMcpManifest _manifest;
    readonly List<AiManagedMcpServer> _kept = [];

    DirectMcpRegistration(DirectMcpScope scope, DirectMcpLocations locations)
    {
        _scope = scope;
        _locations = locations;
        _root = locations.Root(scope);
        _members = new(_root);
        _manifest = DirectMcpManifest.Read(_root);
    }

    /// <summary>Gets the planned changes.</summary>
    internal List<DirectMcpChange> Changes { get; } = [];

    /// <summary>Gets entries that block the plan; nothing is written while any exist.</summary>
    internal IReadOnlyList<string> Conflicts => _members.Conflicts;

    /// <summary>Gets clients that cannot be registered in the scope, with the reason.</summary>
    internal List<string> Unsupported { get; } = [];

    /// <summary>Gets whether an installation selected only clients that cannot be registered.</summary>
    internal bool NothingRegistrable { get; private set; }

    /// <summary>Plans registering the bridge for clients, updating registrations this command owns.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <param name="clients">The clients; empty selects every client whose configuration exists.</param>
    /// <param name="args">The launch arguments after 'cratis'.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="AiMcpConfigurationInvalid">When no client is selected or found, or a configuration file is unusable.</exception>
    internal static DirectMcpRegistration Install(DirectMcpScope scope, DirectMcpLocations locations, IReadOnlyList<string> clients, IReadOnlyList<string> args)
    {
        var plan = new DirectMcpRegistration(scope, locations);
        var selected = clients.Count > 0 ? Validate(clients) : [.. DirectMcpClients.All.Where(client => DirectMcpClients.IsPresent(client, scope, locations))];
        if (selected.Count == 0)
        {
            throw new AiMcpConfigurationInvalid($"No supported MCP client configuration was found in the {Describe(scope)}; choose one with --client.");
        }
        var desired = new List<AiManagedMcpServer>();
        foreach (var client in selected)
        {
            var rendered = DirectMcpClients.Render(client, scope, locations, args);
            if (rendered.Entry is null) plan.Unsupported.Add($"{client}: {rendered.Unsupported}");
            else desired.Add(rendered.Entry);
        }
        plan.NothingRegistrable = desired.Count == 0;

        // An unsupported client's earlier registration is kept as it is rather than removed.
        plan.Prepare([.. desired.Select(entry => entry.Harness)], desired);
        return plan;
    }

    /// <summary>Plans removing the registrations this command owns.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <param name="clients">The clients; empty selects every owned registration.</param>
    /// <returns>The plan.</returns>
    internal static DirectMcpRegistration Uninstall(DirectMcpScope scope, DirectMcpLocations locations, IReadOnlyList<string> clients)
    {
        var plan = new DirectMcpRegistration(scope, locations);
        var selected = clients.Count > 0 ? Validate(clients) : [.. plan._manifest.Servers.Select(server => server.Harness).Distinct(StringComparer.Ordinal)];
        plan.Prepare(selected, []);
        return plan;
    }

    /// <summary>Reports the registration state of clients.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <param name="clients">The clients; empty reports every client.</param>
    /// <returns>The state of each client.</returns>
    internal static IReadOnlyList<DirectMcpClientStatus> Status(DirectMcpScope scope, DirectMcpLocations locations, IReadOnlyList<string> clients)
    {
        var plan = new DirectMcpRegistration(scope, locations);
        var result = new List<DirectMcpClientStatus>();
        foreach (var client in clients.Count > 0 ? Validate(clients) : DirectMcpClients.All)
        {
            // Only where the registration lives matters here, not what it launches.
            var rendered = DirectMcpClients.Render(client, scope, locations, []);
            if (rendered.Entry is not { } entry)
            {
                result.Add(new(client, null, "unsupported", rendered.Unsupported));
                continue;
            }
            var owned = plan._manifest.Servers.FirstOrDefault(server => server.Harness == client);
            if (owned is not null)
            {
                DirectMcpClients.ValidateOwned(scope, locations, owned);
                var current = plan._members.Get(owned.Path, owned.Collection, owned.Id);
                result.Add(JsonNode.DeepEquals(current, owned.Installed)
                    ? new(client, plan.Display(owned.Path), "registered", Launch(owned))
                    : new(client, plan.Display(owned.Path), "modified", "The registration changed since it was installed; it is left alone."));
                continue;
            }
            result.Add(plan._members.Get(entry.Path, entry.Collection, entry.Id) is null
                ? new(client, plan.Display(entry.Path), "absent", null)
                : new(client, plan.Display(entry.Path), "user-owned", $"'{entry.Collection}.{entry.Id}' was not written by 'cratis direct mcp install'; it is left alone."));
        }
        return result;
    }

    /// <summary>Writes the planned changes and the ownership manifest.</summary>
    /// <param name="operations">The file operations, which may be a dry run.</param>
    /// <exception cref="AiMcpConfigurationInvalid">When the plan has conflicts.</exception>
    internal void Apply(AiFileOperations operations)
    {
        _members.Apply(operations);
        var manifest = new DirectMcpManifest([.. _kept, .. _members.Installed]);
        if (!_manifest.Servers.SequenceEqual(manifest.Servers, OwnershipComparer.Instance)) manifest.Write(_root, operations);
    }

    static List<string> Validate(IReadOnlyList<string> clients)
    {
        var selected = clients.Select(client => client.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToList();
        var unknown = selected.Where(client => !DirectMcpClients.All.Contains(client, StringComparer.Ordinal)).ToArray();
        if (unknown.Length > 0) throw new AiMcpConfigurationInvalid($"Unknown MCP client: {string.Join(", ", unknown)}. Choose from {string.Join(", ", DirectMcpClients.All)}.");
        return selected;
    }

    static string Describe(DirectMcpScope scope) => scope == DirectMcpScope.User ? "home directory" : "project";

    static string Launch(AiManagedMcpServer entry)
    {
        var command = entry.Installed["args"] is JsonArray args
            ? ["cratis", .. args.Select(value => value!.GetValue<string>())]
            : entry.Installed["command"]!.AsArray().Select(value => value!.GetValue<string>());
        return string.Join(' ', command);
    }

    static string Render(string path, string collection, string id, JsonNode? value)
    {
        if (value is null) return string.Empty;
        if (!path.EndsWith(".toml", StringComparison.Ordinal)) return new JsonObject { [id] = value.DeepClone() }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        // The same serialization the Codex document appends, so the dry run shows the exact table.
        TomlArray arguments = [.. value["args"]!.AsArray().Select(item => item!.GetValue<string>())];
        var entry = new Dictionary<string, object>(StringComparer.Ordinal) { ["command"] = value["command"]!.GetValue<string>(), ["args"] = arguments };
        return $"[{collection}.{id}]\n{TomlSerializer.Serialize(entry)}".TrimEnd();
    }

    void Prepare(IReadOnlyList<string> selected, IReadOnlyList<AiManagedMcpServer> desired)
    {
        var previous = _manifest.Servers.Where(server => selected.Contains(server.Harness, StringComparer.Ordinal)).ToList();
        _kept.AddRange(_manifest.Servers.Except(previous));
        _members.Prepare(desired, previous, entry => DirectMcpClients.ValidateOwned(_scope, _locations, entry));
        foreach (var change in _members.Changes)
        {
            var action = (change.Before, change.After) switch
            {
                (_, null) => "remove",
                (null, _) => "add",
                _ => "update"
            };
            Changes.Add(new(change.Harness, Display(change.Path), $"{change.Collection}.{change.Id}", action, Render(change.Path, change.Collection, change.Id, change.After ?? change.Before)));
        }
    }

    string Display(string path) => _scope == DirectMcpScope.User ? $"~/{path}" : path;

    sealed class OwnershipComparer : IEqualityComparer<AiManagedMcpServer>
    {
        internal static readonly OwnershipComparer Instance = new();

        public bool Equals(AiManagedMcpServer? x, AiManagedMcpServer? y) =>
            x is not null && y is not null && x.Harness == y.Harness && x.Path == y.Path && x.Collection == y.Collection && x.Id == y.Id &&
            JsonNode.DeepEquals(x.Installed, y.Installed) && JsonNode.DeepEquals(x.Preimage, y.Preimage);

        public int GetHashCode(AiManagedMcpServer obj) => HashCode.Combine(obj.Harness, obj.Path, obj.Id);
    }
}
