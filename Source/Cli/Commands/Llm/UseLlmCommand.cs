// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Llm;

/// <summary>
/// Configures the language model provider used by Cratis tools like Prologue.
/// </summary>
[LlmDescription("Configures the language model provider (anthropic, openai, or local OpenAI-compatible) that Cratis tools like Prologue use. Stores kind, API key, endpoint, and model in the user configuration. Prompts interactively for missing values; pass --api-key/--endpoint/--model for non-interactive use.")]
[CliCommand("use", "Configure the language model provider to use", Branch = typeof(LlmBranch))]
[CliExample("llm", "use", "anthropic")]
[CliExample("llm", "use", "openai", "--api-key", "sk-...", "--model", "gpt-4o-mini")]
[CliExample("llm", "use", "local", "--endpoint", "http://localhost:11434/v1")]
[LlmOutputAdvice("plain", "Plain outputs a confirmation message.")]
[LlmOption("<KIND>", "string", "Provider kind: anthropic, openai, or local (positional)")]
[LlmOption("--api-key", "string", "API key for the provider. Required for anthropic and openai when not running interactively.")]
[LlmOption("--endpoint", "string", "Endpoint URL. Required for local (OpenAI-compatible, e.g. http://localhost:11434/v1).")]
[LlmOption("--model", "string", "Model to use. Defaults: claude-opus-4-6 (anthropic), gpt-4o-mini (openai).")]
public class UseLlmCommand : AsyncCommand<UseLlmSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, UseLlmSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        if (!LlmKinds.IsValid(settings.Kind))
        {
            OutputFormatter.WriteError(format, $"Unknown language model kind '{settings.Kind}'", $"Valid kinds are: {string.Join(", ", LlmKinds.All)}", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var kind = LlmKinds.Normalize(settings.Kind);
        var interactive = AnsiConsole.Profile.Capabilities.Interactive && !settings.Quiet;

        var apiKey = settings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (!interactive && kind != LlmKinds.Local)
            {
                OutputFormatter.WriteError(format, $"An API key is required for '{kind}'", "Pass it with --api-key, or run in an interactive terminal to be prompted for it", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            if (interactive)
            {
                apiKey = await PromptForApiKey(kind, cancellationToken);
            }
        }

        var endpoint = settings.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint) && kind == LlmKinds.Local)
        {
            if (!interactive)
            {
                OutputFormatter.WriteError(format, "An endpoint is required for 'local'", $"Pass it with --endpoint (e.g. {LlmKinds.DefaultLocalEndpoint}), or run in an interactive terminal to be prompted for it", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            endpoint = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("Endpoint (OpenAI-compatible):").DefaultValue(LlmKinds.DefaultLocalEndpoint),
                cancellationToken);
        }

        var model = settings.Model;
        if (string.IsNullOrWhiteSpace(model) && interactive)
        {
            model = await PromptForModel(kind, cancellationToken);
        }

        SaveConfiguration(kind, endpoint, apiKey, model);

        OutputFormatter.WriteMessage(format, ConfirmationMessage(kind, model));
        return ExitCodes.Success;
    }

    static async Task<string> PromptForApiKey(string kind, CancellationToken cancellationToken)
    {
        var required = kind != LlmKinds.Local;
        var prompt = new TextPrompt<string>(required ? "API key:" : "API key (leave empty if not needed):")
            .PromptStyle("dim")
            .Secret();

        if (!required)
        {
            prompt = prompt.AllowEmpty();
        }

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    static async Task<string> PromptForModel(string kind, CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("Model (leave empty for provider default):").AllowEmpty();
        var defaultModel = LlmKinds.DefaultModelFor(kind);
        if (defaultModel is not null)
        {
            prompt = prompt.DefaultValue(defaultModel);
        }

        return await AnsiConsole.PromptAsync(prompt, cancellationToken);
    }

    static void SaveConfiguration(string kind, string? endpoint, string? apiKey, string? model)
    {
        var config = CliConfiguration.Load();
        config.Llm = new LlmConfiguration
        {
            Kind = kind,
            Endpoint = NullIfEmpty(endpoint),
            ApiKey = NullIfEmpty(apiKey),
            Model = NullIfEmpty(model)
        };
        config.Save();
    }

    static string ConfirmationMessage(string kind, string? model) =>
        string.IsNullOrWhiteSpace(model)
            ? $"Configured '{kind}' as the language model. Show it with: cratis llm show"
            : $"Configured '{kind}' with model '{model}' as the language model. Show it with: cratis llm show";

    static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
