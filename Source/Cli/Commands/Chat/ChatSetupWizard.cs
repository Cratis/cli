// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Interactive wizard for configuring AI provider settings on first use.
/// </summary>
public static class ChatSetupWizard
{
    /// <summary>
    /// Runs the setup wizard and saves the result to the current CLI context.
    /// </summary>
    /// <returns>True if setup completed, false if the user cancelled.</returns>
    public static async Task<bool> RunAsync()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{OutputFormatter.Accent.ToMarkup()}]AI Chat Setup[/]");
        AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Configure an AI provider for the chat command.[/]");
        AnsiConsole.WriteLine();

        var provider = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an AI provider:")
                .AddChoices(
                    ChatClientFactory.Providers.OpenAI,
                    ChatClientFactory.Providers.Anthropic,
                    ChatClientFactory.Providers.Ollama,
                    ChatClientFactory.Providers.AzureOpenAI));

        var defaultModel = ChatClientFactory.DefaultModel(provider);
        var model = AnsiConsole.Prompt(
            new TextPrompt<string>($"Model [{OutputFormatter.Muted.ToMarkup()}]({defaultModel})[/]:")
                .DefaultValue(defaultModel)
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(model))
        {
            model = defaultModel;
        }

        string? apiKey = null;
        string? baseUrl = null;
        var firstAttempt = true;

        while (true)
        {
            if (provider is not ChatClientFactory.Providers.Ollama)
            {
                if (firstAttempt)
                {
                    var envVar = ChatClientFactory.DefaultEnvVar(provider);
                    var envValue = envVar is not null ? Environment.GetEnvironmentVariable(envVar) : null;

                    if (!string.IsNullOrEmpty(envValue))
                    {
                        var useEnv = await AnsiConsole.ConfirmAsync($"Found ${envVar} in environment. Use it?", true);
                        if (useEnv)
                        {
                            apiKey = $"${envVar}";
                        }
                    }
                }

                apiKey ??= AnsiConsole.Prompt(
                    new TextPrompt<string>("API key:")
                        .Secret());
            }

            if (provider is ChatClientFactory.Providers.Ollama)
            {
                baseUrl ??= AnsiConsole.Prompt(
                    new TextPrompt<string>($"Ollama URL [{OutputFormatter.Muted.ToMarkup()}](http://localhost:11434)[/]:")
                        .DefaultValue("http://localhost:11434")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = "http://localhost:11434";
                }
            }
            else if (provider is ChatClientFactory.Providers.AzureOpenAI && firstAttempt)
            {
                baseUrl = AnsiConsole.Prompt(
                    new TextPrompt<string>("Azure OpenAI endpoint URL:"));
            }

            AnsiConsole.WriteLine();
            var verified = false;
            var verifyError = (string?)null;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(new Style(OutputFormatter.Accent))
                .StartAsync("Verifying credentials...", async _ =>
                    (verified, verifyError) = await ChatClientFactory.VerifyAsync(provider, model, apiKey, baseUrl));

            if (verified)
            {
                break;
            }

            AnsiConsole.MarkupLine($"[{OutputFormatter.Danger.ToMarkup()}]Verification failed:[/] {(verifyError ?? "Unknown error").EscapeMarkup()}");
            AnsiConsole.WriteLine();

            if (!await AnsiConsole.ConfirmAsync("Re-enter credentials?", true))
            {
                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Setup cancelled.[/]");
                return false;
            }

            // Reset credentials for retry.
            apiKey = null;
            if (provider is ChatClientFactory.Providers.Ollama)
            {
                baseUrl = null;
            }

            firstAttempt = false;
        }

        if (provider is not ChatClientFactory.Providers.Ollama)
        {
            AnsiConsole.MarkupLine($"[{OutputFormatter.Warning.ToMarkup()}]Data consent:[/] Your prompts and Chronicle data will be sent to [bold]{provider}[/].");
            var consent = await AnsiConsole.ConfirmAsync("Continue?", true);
            if (!consent)
            {
                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Setup cancelled.[/]");
                return false;
            }
        }

        var config = CliConfiguration.Load();
        var context = config.GetCurrentContext();
        context.AiProvider = provider;
        context.AiModel = model;
        context.AiApiKey = apiKey;
        context.AiBaseUrl = baseUrl;
        context.AiDataConsent = true;
        config.Save();

        AnsiConsole.WriteLine();
        OutputFormatter.WriteMessage(OutputFormats.Table, $"AI configured: {provider} / {model}");
        return true;
    }
}
