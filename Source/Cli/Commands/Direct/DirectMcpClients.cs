// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Where a Direct MCP registration is written.</summary>
public enum DirectMcpScope
{
    /// <summary>The user's own client configuration, for every project.</summary>
    User = 0,

    /// <summary>The current project's client configuration, shared with everyone using the repository.</summary>
    Project = 1,
}

/// <summary>The directories and platform facts client configuration paths are resolved against.</summary>
/// <param name="Home">The user's home directory.</param>
/// <param name="Project">The project directory.</param>
/// <param name="Platform">"windows", "macos" or "linux".</param>
/// <param name="Environment">Reads an environment variable.</param>
internal sealed record DirectMcpLocations(string Home, string Project, string Platform, Func<string, string?> Environment)
{
    /// <summary>Gets the locations of the current user, directory and operating system.</summary>
    internal static DirectMcpLocations Current => new(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        Directory.GetCurrentDirectory(),
        CurrentPlatform(),
        System.Environment.GetEnvironmentVariable);

    /// <summary>Gets the physical directory configuration paths in a scope are relative to.</summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The directory.</returns>
    internal string Root(DirectMcpScope scope) => AiProjectPaths.PhysicalRoot(scope == DirectMcpScope.User ? Home : Project);

    static string CurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        return OperatingSystem.IsMacOS() ? "macos" : "linux";
    }
}

/// <summary>The rendered registration for one client, or why the client cannot be registered.</summary>
/// <param name="Entry">The owned member to write, or null when unsupported.</param>
/// <param name="Unsupported">The reason the client cannot be registered in the scope.</param>
internal sealed record DirectMcpClientEntry(AiManagedMcpServer? Entry, string? Unsupported);

/// <summary>Native MCP configuration adapters for the clients that can launch the Direct stdio bridge.</summary>
internal static class DirectMcpClients
{
    /// <summary>The member name every client registration uses.</summary>
    internal const string Id = "cratis-direct";

    /// <summary>Gets the client names in the order they are reported.</summary>
    internal static readonly IReadOnlyList<string> All = ["claude", "codex", "copilot", "cursor", "opencode", "pi"];

    static readonly string[] _bridge = ["direct", "mcp"];

    /// <summary>
    /// Gets the launch arguments for the bridge, always pinning both the origin and the tenant: a registration without a
    /// tenant says '--no-tenant', so neither is ever completed from whichever login is active when the client starts it.
    /// </summary>
    /// <param name="url">The origin to pin.</param>
    /// <param name="tenant">The tenant to pin, or null to pin no tenant.</param>
    /// <returns>The arguments after <c language="shell">cratis</c>.</returns>
    internal static IReadOnlyList<string> Arguments(string url, string? tenant)
    {
        var args = new List<string>(_bridge) { "--url", DirectCredentials.OriginOf(DirectTarget.Create(url, null)) };
        if (tenant is null) args.Add("--no-tenant");
        else args.AddRange(["--tenant", DirectTarget.Create("https://cratis.direct", tenant).Tenant!]);
        return args;
    }

    /// <summary>Renders the registration for a client in a scope.</summary>
    /// <param name="client">The client name.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <param name="args">The launch arguments after <c language="shell">cratis</c>.</param>
    /// <returns>The member to own, or why the client is unsupported.</returns>
    internal static DirectMcpClientEntry Render(string client, DirectMcpScope scope, DirectMcpLocations locations, IReadOnlyList<string> args)
    {
        var (paths, unsupported) = Paths(client, scope, locations);
        if (unsupported is not null) return new(null, unsupported);
        var path = client == "opencode" ? OpenCodePath(locations.Root(scope), paths) : paths[0];
        return new(new(client, path, Collection(client), Id, Value(client, args)), null);
    }

    /// <summary>Gets the configuration file a client would be registered in, for reporting.</summary>
    /// <param name="client">The client name.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <returns>The candidate paths, or why the client is unsupported.</returns>
    internal static (IReadOnlyList<string> Paths, string? Unsupported) Paths(string client, DirectMcpScope scope, DirectMcpLocations locations) =>
        (client, scope) switch
        {
            ("claude", DirectMcpScope.User) => string.IsNullOrWhiteSpace(locations.Environment("CLAUDE_CONFIG_DIR"))
                ? ([".claude.json"], null)
                : ([], "CLAUDE_CONFIG_DIR relocates Claude Code's user configuration; register 'cratis direct mcp' with 'claude mcp add --scope user' instead."),
            ("claude", DirectMcpScope.Project) => ([".mcp.json"], null),
            ("codex", DirectMcpScope.User) => HomeRelative(locations, locations.Environment("CODEX_HOME"), ".codex", "config.toml", "CODEX_HOME"),
            ("codex", DirectMcpScope.Project) => ([".codex/config.toml"], null),
            ("cursor", _) => ([".cursor/mcp.json"], null),
            ("copilot", DirectMcpScope.User) => VisualStudioCode(locations),
            ("copilot", DirectMcpScope.Project) => ([".vscode/mcp.json"], null),
            ("opencode", DirectMcpScope.User) => OpenCode(HomeRelative(locations, locations.Environment("XDG_CONFIG_HOME"), ".config", "opencode", "XDG_CONFIG_HOME")),
            ("opencode", DirectMcpScope.Project) => (["opencode.json", "opencode.jsonc"], null),
            ("pi", _) => ([], "pi has no native MCP configuration; load the stdio bridge 'cratis direct mcp' through a Pi MCP extension instead."),
            _ => ([], $"{client} is not a supported MCP client; choose one of {string.Join(", ", All)}.")
        };

    /// <summary>
    /// Rejects an ownership record that does not describe a registration this command could have written. A record is
    /// checked against every location the command may have written for the client, not only the one the environment
    /// selects now: a registration written before CODEX_HOME, XDG_CONFIG_HOME or APPDATA changed stays valid.
    /// </summary>
    /// <param name="scope">The scope the record belongs to.</param>
    /// <param name="entry">The ownership record.</param>
    /// <exception cref="AiMcpConfigurationInvalid">When the record is not an allowed registration.</exception>
    internal static void ValidateOwned(DirectMcpScope scope, AiManagedMcpServer entry)
    {
        if (!All.Contains(entry.Harness, StringComparer.Ordinal) || !IsWritable(entry.Harness, scope, entry.Path) ||
            entry.Collection != Collection(entry.Harness) || entry.Id != Id || entry.Preimage is not null)
        {
            throw new AiMcpConfigurationInvalid($"Invalid Direct MCP ownership record for {entry.Harness} in {DirectMcpManifest.RelativePath}.");
        }
    }

    /// <summary>
    /// Gets whether the command could have written a client's registration to a path in a scope, under any environment:
    /// a fixed location, or, for a configuration directory an environment variable relocates, any directory inside the
    /// home directory that ends in the client's own file.
    /// </summary>
    /// <param name="client">The client name.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="path">The recorded path, relative to the scope's root.</param>
    /// <returns>True when the path is one the command writes for the client.</returns>
    internal static bool IsWritable(string client, DirectMcpScope scope, string path) => IsPlainRelative(path) && (client, scope) switch
    {
        // CLAUDE_CONFIG_DIR is never written, so only the default location is.
        ("claude", DirectMcpScope.User) => path == ".claude.json",
        ("claude", DirectMcpScope.Project) => path == ".mcp.json",
        ("codex", DirectMcpScope.User) => IsRelocated(path, "config.toml"),
        ("codex", DirectMcpScope.Project) => path == ".codex/config.toml",
        ("cursor", _) => path == ".cursor/mcp.json",
        ("copilot", DirectMcpScope.User) => path == "Library/Application Support/Code/User/mcp.json" || IsRelocated(path, "Code/User/mcp.json"),
        ("copilot", DirectMcpScope.Project) => path == ".vscode/mcp.json",
        ("opencode", DirectMcpScope.User) => IsRelocated(path, "opencode/opencode.json") || IsRelocated(path, "opencode/opencode.jsonc"),
        ("opencode", DirectMcpScope.Project) => path == "opencode.json" || path == "opencode.jsonc",
        _ => false
    };

    /// <summary>Gets whether a client appears to be in use, so a registration without --client targets it.</summary>
    /// <param name="client">The client name.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="locations">The locations to resolve against.</param>
    /// <returns>True when the client's configuration file or directory exists.</returns>
    internal static bool IsPresent(string client, DirectMcpScope scope, DirectMcpLocations locations)
    {
        var (paths, unsupported) = Paths(client, scope, locations);
        if (unsupported is not null) return false;
        var root = locations.Root(scope);
        return paths.Any(path =>
        {
            var full = Path.Combine(root, path);
            var directory = Path.GetDirectoryName(path);
            return File.Exists(full) || (!string.IsNullOrEmpty(directory) && Directory.Exists(Path.Combine(root, directory)));
        });
    }

    internal static string Collection(string client) => client switch
    {
        "copilot" => "servers",
        "codex" => "mcp_servers",
        "opencode" => "mcp",
        _ => "mcpServers"
    };

    static JsonObject Value(string client, IReadOnlyList<string> args) => client switch
    {
        "codex" => new JsonObject { ["command"] = "cratis", ["args"] = Array(args) },
        "opencode" => new JsonObject { ["type"] = "local", ["command"] = Array(["cratis", .. args]), ["enabled"] = true },
        _ => new JsonObject { ["type"] = "stdio", ["command"] = "cratis", ["args"] = Array(args) }
    };

    static bool IsPlainRelative(string path) =>
        path.Length > 0 && !Path.IsPathRooted(path) && !path.Contains('\\') && !path.Contains(':') &&
        path.Split('/').All(segment => segment.Length > 0 && segment != "." && segment != "..");

    /// <summary>Gets whether a path is a client's file in a directory beneath the root, as a relocated directory produces.</summary>
    /// <param name="path">The plain relative path.</param>
    /// <param name="file">The client's file, relative to its configuration directory.</param>
    /// <returns>True when the path ends in the file below at least one directory.</returns>
    static bool IsRelocated(string path, string file) => path.EndsWith($"/{file}", StringComparison.Ordinal);

    static JsonArray Array(IEnumerable<string> values) => [.. values.Select(value => (JsonNode?)JsonValue.Create(value))];

    static (IReadOnlyList<string> Paths, string? Unsupported) OpenCode((IReadOnlyList<string> Paths, string? Unsupported) directory) =>
        directory.Unsupported is not null ? directory : ([$"{directory.Paths[0]}/opencode.json", $"{directory.Paths[0]}/opencode.jsonc"], null);

    static (IReadOnlyList<string> Paths, string? Unsupported) VisualStudioCode(DirectMcpLocations locations) => locations.Platform switch
    {
        "macos" => (["Library/Application Support/Code/User/mcp.json"], null),
        "windows" => HomeRelative(locations, locations.Environment("APPDATA"), "AppData/Roaming", "Code/User/mcp.json", "APPDATA"),
        _ => HomeRelative(locations, locations.Environment("XDG_CONFIG_HOME"), ".config", "Code/User/mcp.json", "XDG_CONFIG_HOME")
    };

    /// <summary>
    /// Resolves a configuration directory that an environment variable may relocate. Only a location inside the home
    /// directory is supported, because ownership is recorded relative to it.
    /// </summary>
    /// <param name="locations">The locations to resolve against.</param>
    /// <param name="overridden">The relocated directory, or null.</param>
    /// <param name="fallback">The default directory relative to the home directory.</param>
    /// <param name="file">The file within the directory.</param>
    /// <param name="variable">The environment variable that relocates the directory.</param>
    /// <returns>The home-relative path, or why it is unsupported.</returns>
    static (IReadOnlyList<string> Paths, string? Unsupported) HomeRelative(DirectMcpLocations locations, string? overridden, string fallback, string file, string variable)
    {
        if (string.IsNullOrWhiteSpace(overridden)) return ([$"{fallback}/{file}"], null);
        var relative = Path.GetRelativePath(locations.Home, overridden).Replace('\\', '/');
        return Path.IsPathRooted(relative) || relative == "." || relative.Split('/').Contains("..", StringComparer.Ordinal)
            ? ([], $"{variable} points outside the home directory; register 'cratis direct mcp' in that client's configuration yourself.")
            : ([$"{relative}/{file}"], null);
    }

    static string OpenCodePath(string root, IReadOnlyList<string> paths)
    {
        var json = File.Exists(Path.Combine(root, paths[0]));
        var jsonc = File.Exists(Path.Combine(root, paths[1]));
        if (json && jsonc) throw new AiMcpConfigurationInvalid($"Both {paths[0]} and {paths[1]} exist; remove one before registering MCP.");
        return jsonc ? paths[1] : paths[0];
    }
}
