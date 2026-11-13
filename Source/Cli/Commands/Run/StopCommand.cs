// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Stops a Stage sandbox that <c language="csharp">cratis run</c> started, typically one left running by <c language="csharp">--detach</c>.
/// </summary>
[LlmDescription("Stops a Stage sandbox started by 'cratis run' (typically with --detach), by the container name the run reported. Only containers named cratis-stage-* can be stopped. With no name it stops the only running sandbox; with several it lists them and exits with a validation error unless --all is given. Exits with a not-found error when no sandbox is running, a connection error when Docker is not available, and a server error when a container is still running after 30 seconds.")]
[CommandEffect(CommandEffect.Local)]
[CliCommand("stop", "Stop a Stage sandbox started by cratis run")]
[CliExample("stop")]
[CliExample("stop", "cratis-stage-a1b2c3d4")]
[CliExample("stop", "--all")]
[LlmOption("[NAME]", "string", "Name of the sandbox to stop (cratis-stage-...). Optional when exactly one sandbox is running.")]
[LlmOption("--all", "bool", "Stop every running Stage sandbox.")]
public class StopCommand : AsyncCommand<StopSettings>
{
    static readonly TimeSpan _stopTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, StopSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        if ((await DockerCli.Execute(StageContainer.BuildListArguments(), cancellationToken)).ExitCode != 0)
        {
            OutputFormatter.WriteError(format, "Docker is not available", "Ensure Docker is installed, running and the 'docker' command is on your PATH", ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }

        var (targets, error) = await Resolve(settings, cancellationToken);
        if (error is not null)
        {
            OutputFormatter.WriteError(format, error.Message, error.Suggestion, error.Code);
            return error.ExitCode;
        }

        var stopped = new List<string>();
        var remaining = new List<string>();
        foreach (var name in targets)
        {
            (await StageSandboxes.Stop(name, _stopTimeout) ? stopped : remaining).Add(name);
        }

        if (remaining.Count > 0)
        {
            OutputFormatter.WriteError(format, $"Still running after {_stopTimeout.TotalSeconds:F0} seconds: {string.Join(", ", remaining)}", "Check 'docker ps'", ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }

        OutputFormatter.WriteObject(format, new { status = "stopped", stopped }, _ =>
        {
            foreach (var name in stopped) AnsiConsole.MarkupLine($"  [{OutputFormatter.Success.ToMarkup()}]✓ Stopped[/] {name.EscapeMarkup()}");
        });
        return ExitCodes.Success;
    }

    static async Task<(IReadOnlyList<string> Targets, StopError? Error)> Resolve(StopSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Name is not null) return ([settings.Name], null);

        var running = await StageSandboxes.Running(cancellationToken);
        return running.Count switch
        {
            0 => ([], new StopError("No Stage sandbox is running", "Start one with 'cratis run'", ExitCodes.NotFoundCode, ExitCodes.NotFound)),
            1 => (running, null),
            _ when settings.All => (running, null),
            _ => ([], new StopError($"Several Stage sandboxes are running: {string.Join(", ", running)}", "Name the one to stop, or pass --all to stop them all", ExitCodes.ValidationErrorCode, ExitCodes.ValidationError))
        };
    }

    sealed record StopError(string Message, string Suggestion, string Code, int ExitCode);
}
