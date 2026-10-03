// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Hands an MCPB to Claude's public install dialog; never reads private extension storage.</summary>
/// <param name="platform">Desktop platform.</param>
/// <param name="open">Public OS open-file operation.</param>
internal sealed class ClaudeDesktopMcp(DesktopMcpPlatform platform, Action<DesktopMcpPlatform, string> open) : IDesktopMcpClient
{
    public string Id => "claude";
    public string DisplayName => "Claude Desktop";
    public bool IsDetected => DesktopMcpApplications.Find(platform, "Claude") is not null;
    public bool IsSupported => platform.SupportsDesktop;
    public string ArtifactSuffix => ".mcpb";
    string Receipt => AiProjectPaths.Within(platform.Home, ".cratis/mcp-desktop/claude.version");

    public string Inspect(string? latest)
    {
        if (!IsSupported) return "unsupported host/platform";
        if (!IsDetected) return "not detected";
        if (!File.Exists(Receipt)) return "detected; installation/version must be checked in Claude Settings > Extensions";
        var version = File.ReadAllText(Receipt).Trim();
        DesktopMcpArtifacts.ValidateVersion(version);
        var update = DesktopMcpArtifacts.HasUpdate(latest, version) ? $"; available update {latest}" : string.Empty;
        return $"bundle {version} handed to host; awaiting host confirmation (installation cannot be verified through a public API){update}";
    }

    public Task<string> Install(string artifact, string version, string? modelRoot, bool dryRun)
    {
        if (!IsSupported) throw new AiMcpConfigurationInvalid("Claude Desktop MCPB installation is supported only on macOS arm64/x64 and Windows x64.");
        DesktopMcpArtifacts.ValidateVersion(version);
        if (dryRun) return Task.FromResult($"Would open Screenplay {version} MCPB in Claude's trust/install dialog. Choose the model folder there.");
        if (!IsDetected) return Task.FromResult($"Claude Desktop was not detected. Install/update Claude, then open {artifact} in Settings > Extensions. No installation is claimed.");
        open(platform, artifact);
        AiFileOperations.Performing.CreateDirectoryFor(Receipt);
        AiFileOperations.Performing.WriteAllTextAtomically(Receipt, version + "\n");
        return Task.FromResult("Bundle opened; complete Claude's trust/install dialog and choose your model folder. Confirm installation in Settings > Extensions. Update Claude if MCPB is not recognized.");
    }

    public async Task<string> Update(string artifact, string version, string? modelRoot, bool dryRun)
    {
        if (File.Exists(Receipt))
        {
            var current = (await File.ReadAllTextAsync(Receipt)).Trim();
            DesktopMcpArtifacts.ValidateVersion(current);
            if (current != version && !DesktopMcpArtifacts.HasUpdate(version, current))
                return "Requested bundle is older than the last handoff; no downgrade performed. Use install with --version for an intentional downgrade.";
        }
        return await Install(artifact, version, modelRoot, dryRun);
    }

    public Task<string> Uninstall(bool dryRun)
    {
        if (!IsSupported) throw new AiMcpConfigurationInvalid("Claude Desktop is unsupported on this platform.");
        if (!dryRun && File.Exists(Receipt)) File.Delete(Receipt);
        return Task.FromResult(dryRun ? "Would forget the CLI handoff receipt; remove Screenplay in Claude Settings > Extensions yourself." : "Remove Screenplay in Claude Settings > Extensions. Only the CLI handoff receipt was removed; Claude's private storage is untouched.");
    }
}
