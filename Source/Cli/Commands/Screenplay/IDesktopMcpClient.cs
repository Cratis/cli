// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>A user-level desktop provider, separate from project-local AiMcpHarnesses registration.</summary>
internal interface IDesktopMcpClient
{
    string Id { get; }
    string DisplayName { get; }
    bool IsDetected { get; }
    bool IsSupported { get; }
    string ArtifactSuffix { get; }
    string Inspect(string? latest);
    Task<string> Install(string artifact, string version, string? modelRoot, bool dryRun);
    Task<string> Update(string artifact, string version, string? modelRoot, bool dryRun);
    Task<string> Uninstall(bool dryRun);
}
