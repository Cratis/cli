// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Registers a personal marketplace source using OpenAI's documented local plugin mechanism.</summary>
/// <param name="platform">Desktop platform and personal configuration root.</param>
internal sealed class ChatGptDesktopMcp(DesktopMcpPlatform platform) : IDesktopMcpClient
{
    const string Packages = ".codex/plugins/cratis-screenplay";
    const string DirectoryFingerprint = "directory";
    static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public string Id => "chatgpt";
    public string DisplayName => "ChatGPT Desktop";
    public bool IsDetected => DesktopMcpApplications.Find(platform, "ChatGPT") is not null;
    public bool IsSupported => platform.SupportsDesktop;
    public string ArtifactSuffix => "-plugin.zip";
    string MarketplacePath => AiProjectPaths.Within(platform.Home, ".agents/plugins/marketplace.json");
    string Receipt => AiProjectPaths.Within(platform.Home, ".cratis/mcp-desktop/chatgpt.json");
    string MarketplaceContent => File.Exists(MarketplacePath) ? File.ReadAllText(MarketplacePath) : "{\n  \"name\": \"cratis\",\n  \"plugins\": []\n}\n";

    public string Inspect(string? latest)
    {
        if (!IsSupported) return "unsupported host/platform";
        var installed = ReadOwned();
        var marketplace = new DesktopMcpMarketplace(MarketplaceContent);
        if (installed is null)
        {
            if (marketplace.Entry is not null) return "foreign Screenplay entry (preserved)";
            return IsDetected ? "detected; not configured" : "not detected";
        }
        VerifyOwned(installed);
        if (installed.CleanupPending) return "source unregistered; package cleanup pending. Disable/remove Screenplay, quit ChatGPT, then rerun uninstall.";
        if (!JsonNode.DeepEquals(marketplace.Entry, installed.Entry)) return "owned package exists but marketplace entry is modified/missing (preserved)";
        var update = DesktopMcpArtifacts.HasUpdate(latest, installed.Version) ? $"; available update {latest}" : string.Empty;
        return $"source registered {installed.Version}; enable/verify in ChatGPT Plugins (host confirmation cannot be inspected){update}";
    }

    public async Task<string> Install(string artifact, string version, string? modelRoot, bool dryRun)
    {
        if (!IsSupported) throw new AiMcpConfigurationInvalid("ChatGPT Desktop local plugins are supported only on macOS arm64/x64 and Windows x64 where the installed host exposes Plugins.");
        DesktopMcpArtifacts.ValidateVersion(version);
        var previous = ReadOwned();
        if (previous?.CleanupPending == true) throw new AiMcpConfigurationInvalid("Finish the pending desktop source uninstall before installing another version.");
        var before = MarketplaceContent;
        var marketplace = new DesktopMcpMarketplace(before);
        marketplace.Set(previous?.Entry, previous?.Entry); // Refuse foreign or changed entries, even in dry-run.
        if (previous is not null) VerifyOwned(previous);
        modelRoot ??= previous?.ModelRoot;
        if (modelRoot is not null)
        {
            modelRoot = Path.GetFullPath(modelRoot);
            if (modelRoot.Contains("${PLUGIN_ROOT}", StringComparison.Ordinal) || modelRoot.Contains("${PLUGIN_DATA}", StringComparison.Ordinal))
                throw new AiMcpConfigurationInvalid("Model folders cannot contain reserved plugin placeholders.");
            var physical = AiProjectPaths.PhysicalRoot(modelRoot);
            if (!string.Equals(physical, modelRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new AiMcpConfigurationInvalid("Choose an existing physical model folder without symbolic links.");
        }
        if (dryRun) return $"Would register Screenplay {version} in the personal marketplace, preserving other plugins. Restart ChatGPT and enable it in Plugins.";
        if (previous is not null && previous.Version == version && previous.ModelRoot == modelRoot)
            return "Source is unchanged. Restart ChatGPT and install/enable Screenplay in Plugins if not already enabled.";
        var relative = $"{Packages}/{version}-{platform.Rid}-{Guid.NewGuid():N}";
        var folder = AiProjectPaths.Within(platform.Home, relative);
        Directory.CreateDirectory(folder);
        var committed = false;
        var receiptBefore = File.Exists(Receipt) ? await File.ReadAllTextAsync(Receipt) : null;
        try
        {
            Extract(artifact, folder);
            var pluginPath = Path.Combine(folder, "plugin.json");
            var plugin = JsonNode.Parse(await File.ReadAllTextAsync(pluginPath))!;
            if (plugin["name"]?.GetValue<string>() != DesktopMcpMarketplace.PluginName || plugin["version"]?.GetValue<string>() != version)
                throw new AiMcpConfigurationInvalid("The downloaded Screenplay plugin identity/version does not match the release.");
            var mcpPath = Path.Combine(folder, "mcp.json");
            var mcp = JsonNode.Parse(await File.ReadAllTextAsync(mcpPath))!;
            var server = mcp["mcpServers"]?["screenplay"] ?? throw new AiMcpConfigurationInvalid("Screenplay plugin has no local server.");
            var binary = $"server/Cratis.Screenplay.Tool{(platform.Os == "win" ? ".exe" : string.Empty)}";
            if (server["type"]?.GetValue<string>() != "stdio" || server["command"]?.GetValue<string>() != $"./{binary}")
                throw new AiMcpConfigurationInvalid("Screenplay plugin does not launch the expected bundled local server.");
            if (modelRoot is not null)
            {
                server["args"] = new JsonArray("mcp", modelRoot);
                await File.WriteAllTextAsync(mcpPath, mcp.ToJsonString(_json));
            }
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Path.Combine(folder, binary), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var entry = new JsonObject
            {
                ["name"] = DesktopMcpMarketplace.PluginName,
                ["source"] = new JsonObject { ["source"] = "local", ["path"] = folder },
                ["policy"] = new JsonObject { ["installation"] = "AVAILABLE", ["authentication"] = "ON_INSTALL" },
                ["category"] = "Productivity"
            };
            var installed = new DesktopMcpPluginInstallation(version, relative, modelRoot, entry, Fingerprints(folder));
            AiFileOperations.Performing.CreateDirectoryFor(Receipt);
            AiFileOperations.Performing.CreateDirectoryFor(MarketplacePath);
            AiFileOperations.Performing.WriteAllTextAtomically(Receipt, JsonSerializer.Serialize(installed, _json));
            AiFileOperations.Performing.WriteAllTextAtomically(MarketplacePath, marketplace.Set(previous?.Entry, entry), () =>
            {
                if (MarketplaceContent != before) throw new AiMcpConfigurationInvalid("Personal marketplace changed during installation; retry after reviewing it.");
            });
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                if (receiptBefore is null) File.Delete(Receipt);
                else AiFileOperations.Performing.WriteAllTextAtomically(Receipt, receiptBefore);
                Directory.Delete(folder, recursive: true); // Only this newly created, unregistered staging folder.
            }
        }

        // Prior immutable package folders remain cached. Uninstall deletes only the active, fingerprinted source.
        return "Source registered. Restart ChatGPT Desktop, open Plugins, and install/enable Screenplay from the Cratis/local marketplace. Confirm tool approval there. No host installation is claimed.";
    }

    public Task<string> Update(string artifact, string version, string? modelRoot, bool dryRun)
    {
        var current = ReadOwned();
        if (current?.CleanupPending == true) throw new AiMcpConfigurationInvalid("Finish the pending desktop source uninstall before updating.");
        if (current is not null && current.Version != version && !DesktopMcpArtifacts.HasUpdate(version, current.Version))
            return Task.FromResult("Requested source is older than the registered version; no downgrade performed. Use install with --version for an intentional downgrade.");
        return Install(artifact, version, modelRoot, dryRun);
    }

    public Task<string> Uninstall(bool dryRun)
    {
        if (!IsSupported) throw new AiMcpConfigurationInvalid("ChatGPT Desktop is unsupported on this platform.");
        var installed = ReadOwned();
        if (installed is null) return Task.FromResult("No Cratis-owned ChatGPT source; foreign entries and host state were left untouched.");
        VerifyOwned(installed);
        var before = MarketplaceContent;
        var marketplace = new DesktopMcpMarketplace(before);
        var content = marketplace.Set(marketplace.Entry is null ? null : installed.Entry, null);
        if (dryRun) return Task.FromResult("Would remove only the unchanged Cratis marketplace entry and its active package. Disable/remove the installed plugin in ChatGPT Plugins.");
        AiFileOperations.Performing.WriteAllTextAtomically(MarketplacePath, content, () =>
        {
            if (MarketplaceContent != before) throw new AiMcpConfigurationInvalid("Marketplace changed during uninstall; nothing was removed.");
        });
        var folder = AiProjectPaths.Within(platform.Home, installed.Folder);
        var remaining = new Dictionary<string, string>(installed.Files, StringComparer.Ordinal);
        try
        {
            foreach (var file in installed.Files.Where(entry => entry.Value != DirectoryFingerprint))
            {
                File.Delete(AiProjectPaths.Within(folder, file.Key));
                remaining.Remove(file.Key);
            }
            foreach (var directory in installed.Files.Where(entry => entry.Value == DirectoryFingerprint).OrderByDescending(entry => entry.Key.Length))
            {
                var path = AiProjectPaths.Within(folder, directory.Key);
                if (!Directory.EnumerateFileSystemEntries(path).Any())
                {
                    Directory.Delete(path);
                    remaining.Remove(directory.Key);
                }
            }
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
            File.Delete(Receipt);
        }
        catch (IOException exception)
        {
            SaveCleanup(installed, remaining);
            throw new AiMcpConfigurationInvalid($"Source unregistered; owned package cleanup is pending: {exception.Message}. Disable/remove Screenplay and quit ChatGPT, then rerun uninstall. Remaining ownership was retained.");
        }
        catch (UnauthorizedAccessException exception)
        {
            SaveCleanup(installed, remaining);
            throw new AiMcpConfigurationInvalid($"Source unregistered; package cleanup needs filesystem permission: {exception.Message}. Restore access and rerun uninstall. Remaining ownership was retained.");
        }
        return Task.FromResult("Cratis-owned source removed. Disable/remove Screenplay in ChatGPT Plugins too; host-managed caches and unrelated plugins are untouched.");
    }

    internal static Dictionary<string, string> Fingerprints(string folder)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in Directory.EnumerateFileSystemEntries(folder))
        {
            var relative = Path.GetRelativePath(folder, item).Replace('\\', '/');
            AiProjectPaths.Within(folder, relative);
            if (Directory.Exists(item))
            {
                result.Add(relative, DirectoryFingerprint);
                foreach (var file in Fingerprints(item)) result.Add($"{relative}/{file.Key}", file.Value);
            }
            else
            {
                using var stream = File.OpenRead(item);
                result.Add(relative, Convert.ToHexString(SHA256.HashData(stream)));
            }
        }
        return result;
    }

    internal static void Extract(string artifact, string folder)
    {
        using var archive = ZipFile.OpenRead(artifact);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            // Reject traversal, links, duplicate names, and oversized expansion before executing anything.
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000) throw new AiMcpConfigurationInvalid("Plugin archive contains a symbolic link.");
            AiProjectPaths.ValidateRelative(entry.FullName.TrimEnd('/'));
            var fullRoot = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
            var target = Path.GetFullPath(Path.Combine(fullRoot, entry.FullName.TrimEnd('/')));
            if (!target.StartsWith(fullRoot, StringComparison.Ordinal)) throw new AiMcpConfigurationInvalid("Plugin archive path escapes the package.");
            AiProjectPaths.Within(folder, Path.GetRelativePath(folder, target).Replace('\\', '/'));
            total += entry.Length;
            if (total > 2L * 1024 * 1024 * 1024) throw new AiMcpConfigurationInvalid("Plugin archive expands beyond the size limit.");
            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(target);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: false);
            }
        }
    }

    internal DesktopMcpPluginInstallation? ReadOwned()
    {
        if (!File.Exists(Receipt)) return null;
        var installed = JsonSerializer.Deserialize<DesktopMcpPluginInstallation>(File.ReadAllText(Receipt)) ?? throw new AiMcpConfigurationInvalid("Invalid desktop MCP ownership receipt.");
        DesktopMcpArtifacts.ValidateVersion(installed.Version);
        if (!installed.Folder.StartsWith($"{Packages}/{installed.Version}-", StringComparison.Ordinal) || installed.Folder[Packages.Length..].Count(value => value == '/') != 1 || installed.Folder.Length < 32 || !Guid.TryParseExact(installed.Folder[^32..], "N", out _) || installed.Files is null || installed.Entry is null)
            throw new AiMcpConfigurationInvalid("Invalid Cratis-owned plugin location.");
        AiProjectPaths.Within(platform.Home, installed.Folder);
        return installed;
    }

    internal void VerifyOwned(DesktopMcpPluginInstallation installed)
    {
        var folder = AiProjectPaths.Within(platform.Home, installed.Folder);
        if (!Directory.Exists(folder))
        {
            if (installed.CleanupPending && installed.Files.Count == 0) return;
            throw new AiMcpConfigurationInvalid("Cratis-owned plugin source is missing; restore it before updating/removing its registration.");
        }
        var actual = Fingerprints(folder);
        if (actual.Count != installed.Files.Count || installed.Files.Any(file => !actual.TryGetValue(file.Key, out var hash) || hash != file.Value))
            throw new AiMcpConfigurationInvalid("Cratis-owned plugin source has been modified; it will not be overwritten or removed.");
    }

    void SaveCleanup(DesktopMcpPluginInstallation installed, Dictionary<string, string> remaining) =>
        AiFileOperations.Performing.WriteAllTextAtomically(Receipt, JsonSerializer.Serialize(installed with { Files = remaining, CleanupPending = true }, _json));
}
