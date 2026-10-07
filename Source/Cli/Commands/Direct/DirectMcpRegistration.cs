// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Ai;
using Microsoft.Win32.SafeHandles;
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
    readonly DirectMcpManifest _read;
    readonly bool _interrupted;
    readonly List<AiManagedMcpServer> _kept = [];

    DirectMcpRegistration(DirectMcpScope scope, DirectMcpLocations locations)
    {
        _scope = scope;
        _locations = locations;
        _root = locations.Root(scope);

        // An owned registration the user removed has nothing left to protect: install adds it again, uninstall forgets it.
        _members = new(_root, removedOwnedIsAbsent: true);
        _read = DirectMcpManifest.Read(_root);
        _interrupted = _read.Pending is not null;
        _manifest = Settle(_read);
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
            // An owned registration is reported where it was written, even when the client now reads another location or
            // cannot be registered any more, because that is where 'uninstall' removes it.
            if (plan._manifest.Servers.FirstOrDefault(server => server.Harness == client) is { } owned)
            {
                DirectMcpClients.ValidateOwned(scope, owned);
                result.Add(plan.OwnedStatus(owned));
                continue;
            }

            // Only where the registration would live matters here, not what it launches.
            var rendered = DirectMcpClients.Render(client, scope, locations, []);
            if (rendered.Entry is not { } entry)
            {
                result.Add(new(client, null, "unsupported", rendered.Unsupported));
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
    /// <param name="protectBackup">Optional backup protection seam for refusal specs.</param>
    /// <exception cref="AiMcpConfigurationInvalid">When the plan has conflicts.</exception>
    internal void Apply(AiFileOperations operations, Action<SafeFileHandle, SafeFileHandle>? protectBackup = null)
    {
        if (Conflicts.Count > 0) throw new AiMcpConfigurationInvalid("Cannot apply an MCP plan with conflicts.");
        if (operations.DryRun || NothingRegistrable) return;
        var manifest = new DirectMcpManifest([.. _kept, .. _members.Installed]);
        if (_members.Changes.Count == 0 && !_interrupted && _read.Pending is null && SameOwnership(_read.Servers, manifest.Servers)) return;
        using var held = DirectMcpManifest.AcquireLock(_root, _locations.Home);

        // Planning is read-only. Under the applying lock, refuse stale plans before any manifest or client mutation.
        _read.ConfirmUnchanged(_root);

        // Refusing a backup must leave both the client files and ownership record untouched.
        BackUpConfigurations(protectBackup);
        var recorded = _read;
        var progress = _manifest.Servers.ToList();
        _members.Apply(operations with { PreserveConfigurationProtection = true }, path =>
        {
            // Publish only completed client writes. A later file failure retains ownership of earlier completed files.
            progress.RemoveAll(entry => entry.Path == path);
            progress.AddRange(manifest.Servers.Where(entry => entry.Path == path));
            var completed = new DirectMcpManifest([.. progress]);
            if (recorded.Pending is null && SameOwnership(recorded.Servers, completed.Servers)) return;
            completed.Write(_root, operations, recorded);
            recorded = completed;
        });
        var settled = !_interrupted && recorded.Pending is null && SameOwnership(recorded.Servers, manifest.Servers);
        if (!settled) manifest.Write(_root, operations, recorded);
    }

    static bool SameOwnership(IReadOnlyList<AiManagedMcpServer> left, IReadOnlyList<AiManagedMcpServer> right) =>
        left.Count == right.Count && left.ToHashSet(OwnershipComparer.Instance).SetEquals(right);

    static bool SameMember(AiManagedMcpServer left, AiManagedMcpServer right) =>
        left.Harness == right.Harness && left.Path == right.Path && left.Collection == right.Collection && left.Id == right.Id;

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

    void BackUpConfigurations(Action<SafeFileHandle, SafeFileHandle>? protectBackup)
    {
        foreach (var relative in _members.Changes.Select(change => change.Path).Distinct(StringComparer.Ordinal))
        {
            var path = AiProjectPaths.Within(_root, relative);
            if (!File.Exists(path)) continue;
            var backup = new FileInfo($"{path}.{Guid.NewGuid():N}.bak");
            var created = false;
            try
            {
                using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var destination = OperatingSystem.IsWindows()
                    ? backup.Create(FileMode.CreateNew, FileSystemRights.Write, FileShare.None, 4096, FileOptions.None, new FileInfo(path).GetAccessControl(AccessControlSections.Access))
                    : new FileStream(backup.FullName, new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.Write,
                        Share = FileShare.None,
                        UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
                    });
                created = true;
                if (protectBackup is not null) protectBackup(source.SafeFileHandle, destination.SafeFileHandle);
                else if (!OperatingSystem.IsWindows()) AiUnixFileOwnership.Copy(source.SafeFileHandle, destination.SafeFileHandle);
                source.CopyTo(destination);
                destination.Flush(true);
            }
            catch
            {
                if (created) File.Delete(backup.FullName);
                throw;
            }
        }
    }

    void Prepare(IReadOnlyList<string> selected, IReadOnlyList<AiManagedMcpServer> desired)
    {
        var previous = _manifest.Servers.Where(server => selected.Contains(server.Harness, StringComparer.Ordinal)).ToList();
        _kept.AddRange(_manifest.Servers.Except(previous));
        _members.Prepare(desired, previous, entry => DirectMcpClients.ValidateOwned(_scope, entry));
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

    /// <summary>
    /// Settles an update that was interrupted after recording its pending values: an owned member whose file already
    /// holds the pending value is owned with that value, and one whose file does not keeps its earlier record.
    /// </summary>
    /// <param name="read">The manifest as read.</param>
    /// <returns>The manifest with nothing pending.</returns>
    /// <exception cref="AiMcpConfigurationInvalid">When a pending value is not an allowed update of an owned member.</exception>
    DirectMcpManifest Settle(DirectMcpManifest read)
    {
        if (read.Pending is not { } pending) return read;
        foreach (var update in pending)
        {
            DirectMcpClients.ValidateOwned(_scope, update);
            if (!read.Servers.Any(owned => SameMember(owned, update)))
            {
                throw new AiMcpConfigurationInvalid($"Invalid Direct MCP ownership record for {update.Harness} in {DirectMcpManifest.RelativePath}.");
            }
        }
        return new([.. read.Servers.Select(owned =>
            pending.FirstOrDefault(update => SameMember(update, owned)) is { } update && JsonNode.DeepEquals(_members.Get(update.Path, update.Collection, update.Id), update.Installed)
                ? update
                : owned)]);
    }

    DirectMcpClientStatus OwnedStatus(AiManagedMcpServer owned)
    {
        var path = Display(owned.Path);
        if (!_members.Contains(owned.Path, owned.Collection, owned.Id))
        {
            return new(owned.Harness, path, "absent", "The registration was removed from the configuration; 'install' adds it again and 'uninstall' forgets it.");
        }
        if (!JsonNode.DeepEquals(_members.Get(owned.Path, owned.Collection, owned.Id), owned.Installed))
        {
            return new(owned.Harness, path, "modified", "The registration changed since it was installed; it is left alone.");
        }
        return new(owned.Harness, path, "registered", Launch(owned) + Relocation(owned));
    }

    /// <summary>Describes where the client reads its configuration now, when that is no longer where it was registered.</summary>
    /// <param name="owned">The owned registration.</param>
    /// <returns>The note to append, or an empty string when the client still reads the recorded file.</returns>
    string Relocation(AiManagedMcpServer owned)
    {
        var (current, unsupported) = DirectMcpClients.Paths(owned.Harness, _scope, _locations);
        if (unsupported is not null) return $" 'install' can no longer register this client: {unsupported} 'uninstall' still removes this registration.";
        return current.Contains(owned.Path, StringComparer.Ordinal) ? string.Empty : $" The client now reads {Display(current[0])}; 'install' moves the registration there.";
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
