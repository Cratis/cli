// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Automatically installs shell completions for the current (or specified) shell.
/// For bash and zsh, appends an eval line to the shell config so completions are always generated fresh.
/// For fish, appends a source line to config.fish for the same dynamic behaviour.
/// </summary>
[CliCommand("install", "Automatically install completions for the current shell (run once after installing cratis)", Branch = typeof(CompletionsBranch))]
[CliExample("completions", "install")]
[CliExample("completions", "install", "--shell", "zsh")]
[CliExample("completions", "install", "--force")]
[LlmOption("--shell", "string", "Target shell: bash, zsh, or fish. Auto-detected from $SHELL if omitted.")]
[LlmOption("--force", "bool", "Remove and re-add the completions line even if already configured.")]
public class CompletionsInstallCommand : Command<CompletionsInstallSettings>
{
    /// <inheritdoc/>
    protected override int Execute(CommandContext context, CompletionsInstallSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var shell = settings.Shell?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(shell))
        {
            shell = ShellCompletionInstaller.DetectShell();
        }

        if (string.IsNullOrWhiteSpace(shell) || !ShellCompletionInstaller.IsSupported(shell))
        {
            OutputFormatter.WriteError(
                format,
                $"Could not detect shell{(string.IsNullOrWhiteSpace(shell) ? string.Empty : $": {shell}")}",
                "Use --shell to specify: bash, zsh, or fish",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        foreach (var action in ShellCompletionInstaller.Install(shell, settings.Force))
        {
            OutputFormatter.WriteMessage(format, action);
        }

        return ExitCodes.Success;
    }
}
