// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.Commands.Studio;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Runs the build loop: takes slices Studio gives this builder, builds each with the agent or generator, verifies it with
/// the kit's commands, commits it and reports the outcome. With <c language="csharp">--modeling</c>, connects the agent to
/// a board instead.
/// </summary>
[CommandEffect(CommandEffect.Mutating)]
[CliCommand(
    "run",
    "Run the build loop: build the slices Studio gives this builder with a coding agent or a generator, verify each with the kit's commands, commit and push it, and report the outcome. With --modeling, connect the agent to a Studio board so people can hand it work in the board chat.",
    Branch = typeof(BuildBranch))]
[CliExample("build", "run", "--agent", "claude")]
[CliExample("build", "run", "--agent", "copilot", "--kit", "./kits/node")]
[CliExample("build", "run", "--exec", "./generate.sh", "--once")]
[CliExample("build", "run", "--modeling", "--agent", "claude", "--board", "0b9d1f0e-6a4c-4c1e-8f5a-3e2d1c0b9a87")]
public sealed class BuildRunCommand : AsyncCommand<BuildRunSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, BuildRunSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        using var interrupt = ShutdownSignal.LinkedTo(cancellationToken);
        try
        {
            var connection = await StudioConnection.Open(interrupt.Token);
            return settings.Modeling
                ? await Model(settings, connection, interrupt.Token)
                : await Build(settings, connection, interrupt.Token);
        }
        catch (StudioBuilderRefused refused)
        {
            OutputFormatter.WriteError(format, refused.Message, refused.CredentialRejected ? "Register again with 'cratis build connect --force'." : null, ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception exception) when (exception is StudioNotConnected or InvalidBuildKit or ArgumentException or ModelingSessionRefused)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
        catch (Exception exception) when (exception is HttpRequestException or StudioMcpError or DirectAuthError)
        {
            OutputFormatter.WriteError(format, exception.Message, errorCode: ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }
    }

    static async Task<int> Build(BuildRunSettings settings, StudioConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.IsRegistered) throw new StudioNotConnected("This machine is not registered as a builder. Run 'cratis build connect' first.");
        var kit = BuildHostFactory.Kit(settings);
        var git = new BuildGit(settings.ProjectPath);
        if (!await git.IsRepository(cancellationToken)) throw new ArgumentException($"{settings.ProjectPath} is not a Git repository; the builder commits what it builds.");
        var pending = (await git.Changes(cancellationToken)).Where(path => path != kit?.Conventions).ToList();
        if (pending.Count > 0) throw new ArgumentException($"The project has uncommitted changes ({string.Join(", ", pending.Take(5))}). Commit or stash them first, so a build commits only its own work.");

        var options = await BuildHostFactory.Create(settings, kit, !settings.NoPush, connection, cancellationToken);
        var host = new BuildHost(options);
        var loop = new BuildLoop(connection.Builder, host.Build, options.Log);
        var built = await loop.Run(BuilderManifests.For(kit, settings.Exec is null ? "agent" : "generator"), settings.Once, cancellationToken);
        BuildHostFactory.Log($"Stopped after building {built} slice{(built == 1 ? string.Empty : "s")}.");
        return ExitCodes.Success;
    }

    static async Task<int> Model(BuildRunSettings settings, StudioConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Mcp is not { } mcp) throw new StudioNotConnected("Modeling needs Studio's tools. Run 'cratis build connect --mcp-client-id <id>' with the organization's MCP credential first.");
        var kit = BuildHostFactory.Kit(settings);
        var options = await BuildHostFactory.Create(settings, kit, false, connection, cancellationToken);
        var workspace = Path.Combine(options.WorkspaceRoot, $"modeling-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspace);

        var studioTools = options.Tools(workspace).Where(tool => tool.Name == BuildSessionTools.Studio).ToList();
        ModelingAgent Start(string sessionId)
        {
            var session = new BuildAgentSession(
                settings.ProjectPath,
                workspace,
                kit is null ? string.Empty : kit.ReadText(kit.Instructions),
                options.Guidance,
                kit?.Skills is { } skills ? kit.PathOf(skills) : null,
                [.. studioTools.Select(tool => tool with { Headers = new Dictionary<string, string> { [ModelingSession.SessionHeader] = sessionId } })],
                new BuildPermissions(settings.ProjectPath, [], [], [workspace, .. options.Guidance is { } guidance ? [guidance.Root] : Array.Empty<string>()], []),
                options.Log)
            {
                MayChangeTools = false
            };
            return new(options.Agent(session), session);
        }

        var name = settings.Name ?? BuildAgents.ProductName(settings.Agent ?? BuildProjectSettings.Read(settings.ProjectPath)?.Agent ?? BuildAgents.Claude);
        var modeling = new ModelingSessionOptions(ModelingSessionOptions.BoardFrom(settings.Board)!, name, settings.Unattended);
        var answered = await new ModelingSession(mcp.Call, Start, modeling, options.Log).Run(cancellationToken);
        BuildHostFactory.Log($"Disconnected from the board after answering {answered} prompt{(answered == 1 ? string.Empty : "s")}.");
        return ExitCodes.Success;
    }
}
