// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Interactive REPL for chatting with an AI assistant about a Chronicle system.
/// </summary>
/// <param name="client">The AI chat client.</param>
/// <param name="tools">The Chronicle tools to make available.</param>
/// <param name="autoConfirm">Whether to skip confirmation prompts for write operations.</param>
/// <param name="additionalSystemPrompt">Optional additional system prompt to append.</param>
#pragma warning disable IDE0060, CS9113 // autoConfirm reserved for future use
public class ChatRepl(IChatClient client, IReadOnlyList<AITool> tools, bool autoConfirm, string? additionalSystemPrompt = null)
#pragma warning restore IDE0060, CS9113
{
#pragma warning disable MA0136 // Raw string trailing newline is intentional for system prompt
    const string SystemPrompt = """
        You are a Chronicle assistant — an expert on Cratis Chronicle, an event-sourced system.
        You have tools to query and operate on the connected Chronicle server.

        Be concise and direct. Use tools to answer questions about observers, events, projections,
        failed partitions, recommendations, and system health. When diagnosing issues, check
        failed partitions and recommendations proactively.

        For write operations (replay, retry, perform), always explain what you're about to do
        before calling the tool, so the user can make an informed confirmation decision.
        """;
#pragma warning restore MA0136

    readonly List<ChatMessage> _history = [];

    /// <summary>
    /// Sends a single question and prints the response. Used for non-interactive mode.
    /// </summary>
    /// <param name="question">The question to ask.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> AskOnce(string question)
    {
        _history.Add(new(ChatRole.User, question));
        return await SendAndPrint();
    }

    /// <summary>
    /// Starts the interactive REPL loop.
    /// </summary>
    /// <returns>The exit code.</returns>
    public async Task<int> RunLoop()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[{OutputFormatter.Accent.ToMarkup()}]Chronicle Chat[/] — type your question, or /help for commands.");
        AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Connected to the current context. Type /quit to exit.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>($"[{OutputFormatter.Accent.ToMarkup()}]>[/]")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.StartsWith('/'))
            {
                var shouldContinue = HandleSlashCommand(input.Trim());
                if (!shouldContinue)
                {
                    return ExitCodes.Success;
                }

                continue;
            }

            _history.Add(new(ChatRole.User, input));
            var exitCode = await SendAndPrint();
            if (exitCode != ExitCodes.Success)
            {
                return exitCode;
            }
        }
    }

    bool HandleSlashCommand(string input)
    {
        var command = input.ToLowerInvariant().Split(' ', 2)[0];
        switch (command)
        {
            case "/quit" or "/exit" or "/q":
                return false;

            case "/clear":
                _history.Clear();
                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Conversation cleared.[/]");
                return true;

            case "/tools":
                AnsiConsole.MarkupLine($"[{OutputFormatter.Accent.ToMarkup()}]Available tools:[/]");
                foreach (var tool in tools)
                {
                    if (tool is AIFunction func)
                    {
                        var prefix = ChronicleChatTools.WriteToolNames.Contains(func.Name) ? "[yellow]*[/] " : "  ";
                        AnsiConsole.MarkupLine($"{prefix}[bold]{func.Name.EscapeMarkup()}[/] — {(func.Description ?? string.Empty).EscapeMarkup()}");
                    }
                }

                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]* = requires confirmation[/]");
                return true;

            case "/context":
                var config = CliConfiguration.Load();
                var ctx = config.GetCurrentContext();
                OutputFormatter.WriteLabel("Context", config.ActiveContextName);
                OutputFormatter.WriteLabel("Server", ctx.Server ?? "(default)");
                OutputFormatter.WriteLabel("Event Store", ctx.EventStore ?? "(default)");
                OutputFormatter.WriteLabel("Namespace", ctx.Namespace ?? "(default)");
                OutputFormatter.WriteLabel("AI Provider", ctx.AiProvider ?? "(not set)");
                OutputFormatter.WriteLabel("AI Model", ctx.AiModel ?? "(not set)");
                return true;

            case "/help":
                AnsiConsole.MarkupLine($"[{OutputFormatter.Accent.ToMarkup()}]Commands:[/]");
                AnsiConsole.MarkupLine("  /quit      Exit the chat");
                AnsiConsole.MarkupLine("  /clear     Clear conversation history");
                AnsiConsole.MarkupLine("  /tools     List available Chronicle tools");
                AnsiConsole.MarkupLine("  /context   Show current connection context");
                AnsiConsole.MarkupLine("  /help      Show this help");
                return true;

            default:
                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]Unknown command: {command.EscapeMarkup()}. Type /help for available commands.[/]");
                return true;
        }
    }

    async Task<int> SendAndPrint()
    {
        var systemPrompt = additionalSystemPrompt is not null
            ? $"{SystemPrompt}\n\n{additionalSystemPrompt}"
            : SystemPrompt;

        var options = new ChatOptions
        {
            Tools = [.. tools]
        };

        try
        {
            AnsiConsole.WriteLine();
            var fullResponse = new System.Text.StringBuilder();

            await foreach (var update in client.GetStreamingResponseAsync(
                [new ChatMessage(ChatRole.System, systemPrompt), .. _history],
                options))
            {
                if (update.Text is not null)
                {
                    Console.Write(update.Text);
                    fullResponse.Append(update.Text);
                }
            }

            Console.WriteLine();
            AnsiConsole.WriteLine();

            if (fullResponse.Length > 0)
            {
                _history.Add(new(ChatRole.Assistant, fullResponse.ToString()));
            }

            return ExitCodes.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{OutputFormatter.Danger.ToMarkup()}]Error: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]You can continue the conversation or type /quit to exit.[/]");
            AnsiConsole.WriteLine();
            return ExitCodes.Success;
        }
    }
}
