// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Shows the configured language model provider.
/// </summary>
[LlmDescription("Shows the configured language model provider: kind, endpoint, model, and a masked API key. Use to verify what Cratis tools like Prologue will use.")]
[CliCommand("show", "Show the configured language model", Branch = typeof(LlmBranch))]
[CliExample("llm", "show")]
[LlmOutputAdvice("json", "JSON is structured for key-value parsing. The API key is always masked.")]
public class ShowLlmCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var llm = CliConfiguration.Load().Llm;

        if (llm is null || string.IsNullOrWhiteSpace(llm.Kind))
        {
            OutputFormatter.WriteMessage(format, "No language model configured. Configure one with: cratis llm use <kind>");
            return Task.FromResult(ExitCodes.Success);
        }

        var maskedKey = string.IsNullOrWhiteSpace(llm.ApiKey) ? null : ApiKeyMask.Mask(llm.ApiKey);

        OutputFormatter.WriteObject(
            format,
            new
            {
                llm.Kind,
                llm.Endpoint,
                llm.Model,
                ApiKey = maskedKey
            },
            _ =>
        {
            AnsiConsole.MarkupLine($"[bold]Kind:[/]     {OrNotSet(llm.Kind).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Endpoint:[/] {OrNotSet(llm.Endpoint).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Model:[/]    {OrNotSet(llm.Model).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]API Key:[/]  {OrNotSet(maskedKey).EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }

    static string OrNotSet(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(not set)" : value;
}
