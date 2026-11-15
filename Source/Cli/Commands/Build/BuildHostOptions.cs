// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// How a host builds orders.
/// </summary>
/// <param name="Project">The project folder the slices are built in.</param>
/// <param name="Kit">The build kit, or <see langword="null"/> for a generator that needs none.</param>
/// <param name="Verify">Verification commands to run besides the kit's.</param>
/// <param name="Push">Whether a commit is pushed to <c language="csharp">origin</c>.</param>
/// <param name="Guidance">The Screenplay session guidance, or <see langword="null"/>.</param>
/// <param name="Tools">Gives the MCP servers of a session, from the folder the order's Screenplay is written to.</param>
/// <param name="Agent">Starts the agent or generator of a session.</param>
/// <param name="Redaction">Removes credentials from what the host logs and reports.</param>
/// <param name="Log">Where the host writes what it is doing.</param>
public sealed record BuildHostOptions(
    string Project,
    BuildKit? Kit,
    IReadOnlyList<string> Verify,
    bool Push,
    ScreenplaySessionGuidance? Guidance,
    Func<string, IReadOnlyList<BuildAgentTool>> Tools,
    Func<BuildAgentSession, IBuildAgent> Agent,
    BuildRedaction Redaction,
    Action<string> Log)
{
    /// <summary>
    /// Gets the folder workspaces are created in, outside the project.
    /// </summary>
    public string WorkspaceRoot { get; init; } = Path.Combine(Path.GetTempPath(), "cratis-build");
}
