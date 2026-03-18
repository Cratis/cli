// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Generates shell completion scripts for bash, zsh, or fish.
/// </summary>
public class CompletionsCommand : Command<CompletionsSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, CompletionsSettings settings, CancellationToken cancellationToken)
    {
        var shell = settings.Shell.ToLowerInvariant();
        var script = shell switch
        {
            "bash" => BashCompletionGenerator.Generate(),
            "zsh" => ZshCompletionGenerator.Generate(),
            "fish" => FishCompletionGenerator.Generate(),
            _ => null
        };

        if (script is null)
        {
            OutputFormatter.WriteError(
                settings.ResolveOutputFormat(),
                $"Unsupported shell: {settings.Shell}",
                "Supported shells: bash, zsh, fish",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        Console.Out.Write(script);

        var hint = shell switch
        {
            "bash" => "# Add to ~/.bashrc:  eval \"$(cratis completions bash)\"",
            "zsh" => "# Add to ~/.zshrc:   eval \"$(cratis completions zsh)\"",
            "fish" => "# Run once:          cratis completions fish | source",
            _ => string.Empty
        };

        Console.Error.WriteLine(hint);
        return ExitCodes.Success;
    }
}
