// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Learns an existing codebase's conventions once. On the first build in a project the kit did not scaffold, the agent
/// reads the project and records its conventions at the kit's path, changing nothing else; every later build passes that
/// path as <c language="csharp">{conventions}</c>. Deleting the file has the conventions learned again.
/// </summary>
/// <param name="options">How the host builds.</param>
public sealed class BuildConventions(BuildHostOptions options)
{
    /// <summary>
    /// The project file a kit's scaffold is marked with, so the host knows the project is not an existing codebase.
    /// </summary>
    public const string ScaffoldedMarker = ".cratis/build/scaffolded";

    /// <summary>
    /// Makes sure the conventions are recorded, learning them when needed.
    /// </summary>
    /// <param name="order">The order about to be built.</param>
    /// <param name="workspace">The order's workspace.</param>
    /// <param name="cancellationToken">Stops the agent.</param>
    /// <returns>The project path of the recorded conventions, or <see langword="null"/> when the kit records none.</returns>
    public async Task<string?> Ensure(BuildOrder order, BuildWorkspace workspace, CancellationToken cancellationToken)
    {
        if (options.Kit is not { Learn: { } learn, Conventions: { } conventions } kit) return null;
        var recorded = Path.Combine(options.Project, conventions);
        if (File.Exists(recorded)) return conventions;
        if (File.Exists(Path.Combine(options.Project, ScaffoldedMarker))) return null;

        options.Log($"Learning the project's conventions into {conventions}; this happens once.");
        var session = new BuildAgentSession(
            options.Project,
            workspace.Root,
            kit.ReadText(kit.Instructions),
            options.Guidance,
            kit.Skills is { } skills ? kit.PathOf(skills) : null,
            [],
            new BuildPermissions(options.Project, [conventions], [], [workspace.Root], []),
            line => options.Log(options.Redaction.Redact(line)));

        var values = BuildKitPlaceholders.For(order, workspace.Order, workspace.Outcome, conventions);
        await using var agent = options.Agent(session);
        var reply = await agent.Prompt(BuildPrompts.ForLearning(kit.ReadText(learn), values, conventions), new Dictionary<string, string>(), cancellationToken);
        if (!reply.Succeeded || !File.Exists(recorded))
        {
            options.Log("The conventions were not recorded; building without them.");
            return null;
        }

        return conventions;
    }
}
