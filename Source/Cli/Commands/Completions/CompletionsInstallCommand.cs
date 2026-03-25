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
[LlmOption("--shell", "string", "Target shell: bash, zsh, or fish. Auto-detected from $SHELL if omitted.")]
public class CompletionsInstallCommand : Command<CompletionsInstallSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, CompletionsInstallSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var shell = settings.Shell?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(shell))
        {
            var shellPath = Environment.GetEnvironmentVariable("SHELL") ?? string.Empty;
            shell = Path.GetFileName(shellPath).ToLowerInvariant();
        }

        return shell switch
        {
            "bash" => InstallEval(ResolveHome(".bashrc"), "eval \"$(cratis completions bash)\"", format),
            "zsh" => InstallEval(ResolveHome(".zshrc"), "eval \"$(cratis completions zsh)\"", format),
            "fish" => InstallEval(ResolveHome(".config", "fish", "config.fish"), "cratis completions fish | source", format),
            _ => UnknownShell(format, shell)
        };
    }

    static int InstallEval(string configFile, string line, string format)
    {
        if (File.Exists(configFile))
        {
            var existing = File.ReadAllText(configFile);
            if (existing.Contains("cratis completions", StringComparison.Ordinal))
            {
                OutputFormatter.WriteMessage(format, $"Completions already configured in {configFile} — nothing to do.");
                return ExitCodes.Success;
            }
        }
        else
        {
            var dir = Path.GetDirectoryName(configFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        File.AppendAllText(configFile, $"\n{line}\n");

        OutputFormatter.WriteMessage(format, $"Added to {configFile}");
        OutputFormatter.WriteMessage(format, $"Reload with:  source {configFile}");
        return ExitCodes.Success;
    }

    static int UnknownShell(string format, string detected)
    {
        OutputFormatter.WriteError(
            format,
            $"Could not detect shell{(string.IsNullOrWhiteSpace(detected) ? string.Empty : $": {detected}")}",
            "Use --shell to specify: bash, zsh, or fish",
            ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    static string ResolveHome(params string[] segments) =>
        Path.Combine(
            [Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), .. segments]);
}
