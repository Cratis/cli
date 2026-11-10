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
    /// <returns>The host's options.</returns>
    /// <exception cref="ArgumentException">Neither an agent nor a generator is given or recorded.</exception>
    public static async Task<BuildHostOptions> Create(BuildAgentSettings settings, BuildKit? kit, bool push, StudioConnection? connection, CancellationToken cancellationToken)
    {
        var agent = settings.Agent ?? BuildProjectSettings.Read(settings.ProjectPath)?.Agent;
        if (agent is null && settings.Exec is null) throw new ArgumentException("Name the agent with --agent, or a generator with --exec.");

        var guidance = settings.NoGuidance ? null : await Guidance(connection, cancellationToken);
        var studioToken = connection?.Mcp is { } mcp ? await mcp.Token(cancellationToken) : null;
        var redaction = new BuildRedaction([.. connection?.Secrets ?? [], studioToken]);
        var studioMcp = connection?.Configuration.McpUrl;

        return new BuildHostOptions(
            settings.ProjectPath,
            kit,
            settings.Verify,
            push,
            guidance,
            model => BuildSessionTools.For(model, studioMcp, studioToken),
            session => settings.Exec is { } exec ? new ExecGenerator(session, exec) : BuildAgents.Create(agent!, settings.AgentCommand, session),
            redaction,
            line => Log(redaction.Redact(line)));
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
