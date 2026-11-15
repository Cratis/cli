// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Composes a host from a command's options: the kit given or installed in the project, the agent or generator, the
/// Screenplay guidance, and the session tools with Studio's when the organization's MCP credential is configured.
/// </summary>
internal static class BuildHostFactory
{
    /// <summary>
    /// Writes a line of what the builder is doing to the standard error, so standard output stays for documents.
    /// </summary>
    /// <param name="line">The line, already free of credentials.</param>
    public static void Log(string line) => Console.Error.WriteLine(line);

    /// <summary>
    /// Loads the kit named by the options, or the one installed in the project.
    /// </summary>
    /// <param name="settings">The options.</param>
    /// <returns>The kit, or <see langword="null"/> when there is none.</returns>
    /// <exception cref="InvalidBuildKit">The kit cannot be used.</exception>
    public static BuildKit? Kit(BuildAgentSettings settings)
    {
        if (settings.Kit is { } kit) return BuildKit.Load(kit);
        var installedKit = Path.Combine(settings.ProjectPath, BuildProjectSettings.KitFolder);
        return Directory.Exists(installedKit) ? BuildKit.Load(installedKit, installed: true) : null;
    }

    /// <summary>
    /// Composes the host.
    /// </summary>
    /// <param name="settings">The options.</param>
    /// <param name="kit">The kit, or <see langword="null"/>.</param>
    /// <param name="push">Whether commits are pushed.</param>
    /// <param name="connection">The Studio connection, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels fetching the guidance and the MCP token.</param>
    /// <param name="ai">The bridge that runs the agent's model calls through Studio, or <see langword="null"/>.</param>
    /// <returns>The host's options.</returns>
    /// <exception cref="ArgumentException">Neither an agent nor a generator is given or recorded.</exception>
    public static async Task<BuildHostOptions> Create(BuildAgentSettings settings, BuildKit? kit, bool push, StudioConnection? connection, CancellationToken cancellationToken, StudioAiBridge? ai = null)
    {
        var agent = settings.Agent ?? BuildProjectSettings.Read(settings.ProjectPath)?.Agent;
        if (settings.ResolvedLevel is { } level && BuildLevels.NeedsAgent(level) && agent is null)
        {
            throw new ArgumentException(MissingAgent(level));
        }

        if (settings.ResolvedLevel is null && agent is null && settings.Exec is null)
        {
            throw new ArgumentException("Name the agent with --agent, a generator with --exec, or the packaged builder's level with --level.");
        }

        var guidance = settings.NoGuidance ? null : await Guidance(connection, cancellationToken);
        var studioToken = connection?.Mcp is { } mcp ? await mcp.Token(cancellationToken) : null;
        var redaction = new BuildRedaction([.. connection?.Secrets ?? [], studioToken, ai?.Token()]);
        var studioMcp = connection?.Configuration.McpUrl;

        return new BuildHostOptions(
            settings.ProjectPath,
            kit,
            settings.Verify,
            push,
            guidance,
            model => BuildSessionTools.For(model, studioMcp, studioToken),
            session => AgentFor(settings, agent, session),
            redaction,
            line => Log(redaction.Redact(line))) { Ai = ai };
    }

    /// <summary>
    /// Gets why a level cannot build without an agent.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>The message.</returns>
    internal static string MissingAgent(BuildLevel level) => level == BuildLevel.AI
        ? $"The ai level needs the agent that implements the slices from the model: name it with --agent ({string.Join(", ", BuildAgents.Names)})."
        : $"The code-ai level needs the agent that implements the model's behavior on top of the render: name it with --agent ({string.Join(", ", BuildAgents.Names)}).";

    static IBuildAgent AgentFor(BuildAgentSettings settings, string? agent, BuildAgentSession session)
    {
        if (settings.Exec is { } exec) return new ExecGenerator(session, exec);
        if (settings.ResolvedLevel is not { } level) return BuildAgents.Create(agent!, settings.AgentCommand, session);
        var implementer = BuildLevels.NeedsAgent(level) ? BuildAgents.Create(agent!, settings.AgentCommand, session) : null;
        return new CratisLevelBuilder(session, level, settings.ResolvedTarget, implementer);
    }

    static async Task<ScreenplaySessionGuidance?> Guidance(StudioConnection? connection, CancellationToken cancellationToken)
    {
        try
        {
            var studio = connection is null ? null : new Uri(connection.Configuration.Url);
            return await new ScreenplaySessionBundle(StudioConnection.Http, ScreenplaySessionBundle.DefaultCache).Resolve(studio, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or JsonException)
        {
            Log($"The Screenplay session guidance is not available ({exception.Message}); building without it.");
            return null;
        }
    }
}
