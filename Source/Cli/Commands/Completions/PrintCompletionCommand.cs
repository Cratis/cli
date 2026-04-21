// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Prints the shell completion script for bash, zsh, or fish.
/// The command name (bash/zsh/fish) determines which script is generated.
/// </summary>
[CliCommand("bash", "Print the bash completion script to stdout", Branch = typeof(CompletionsBranch))]
[CliCommand("zsh", "Print the zsh completion script to stdout", Branch = typeof(CompletionsBranch))]
[CliCommand("fish", "Print the fish completion script to stdout", Branch = typeof(CompletionsBranch))]
[CliExample("completions", "bash", CommandName = "bash")]
[CliExample("completions", "zsh", CommandName = "zsh")]
[CliExample("completions", "fish", CommandName = "fish")]
public class PrintCompletionCommand : Command<GlobalSettings>
{
    /// <inheritdoc/>
    protected override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var script = context.Name switch
        {
            "bash" => BashCompletionGenerator.Generate(),
            "zsh" => ZshCompletionGenerator.Generate(),
            "fish" => FishCompletionGenerator.Generate(),
            _ => null
        };

        if (script is null)
        {
            OutputFormatter.WriteError(settings.ResolveOutputFormat(), $"Unsupported shell: {context.Name}", "Supported: bash, zsh, fish", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        Console.Out.Write(script);
        return ExitCodes.Success;
    }
}
