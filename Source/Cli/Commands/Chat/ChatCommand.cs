// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Cratis.Cli.Commands.Chronicle;
using Grpc.Core;
using Microsoft.Extensions.AI;

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Interactive AI chat command for querying and operating on a Chronicle system.
/// </summary>
public class ChatCommand : AsyncCommand<ChatSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ChatSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        var provider = settings.Provider ?? ctx.AiProvider;
        var model = settings.Model ?? ctx.AiModel;
        var apiKey = ctx.AiApiKey;
        var baseUrl = ctx.AiBaseUrl;

        if (string.IsNullOrEmpty(provider))
        {
            if (!AnsiConsole.Profile.Out.IsTerminal)
            {
                OutputFormatter.WriteError(format, "No AI provider configured", "Run 'cratis chat' interactively to set up, or use --provider", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            if (!ChatSetupWizard.Run())
            {
                return ExitCodes.Success;
            }

            config = CliConfiguration.Load();
            ctx = config.GetCurrentContext();
            provider = ctx.AiProvider!;
            model = ctx.AiModel;
            apiKey = ctx.AiApiKey;
            baseUrl = ctx.AiBaseUrl;
        }

        model ??= ChatClientFactory.DefaultModel(provider);

        IReadOnlyList<AITool> tools = [];
        CliServiceClient? serviceClient = null;

        if (!settings.NoTools)
        {
            try
            {
                var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
                var managementPort = settings.ResolveManagementPort();
                serviceClient = CliServiceClient.Create(connectionString, managementPort);
                tools = ChronicleChatTools.Create(serviceClient.Services);
            }
            catch (Exception ex) when (ex is RpcException or HttpRequestException or SocketException)
            {
                AnsiConsole.MarkupLine($"[{OutputFormatter.Warning.ToMarkup()}]Could not connect to Chronicle server — chat will work without tools.[/]");
                AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]{ex.Message.EscapeMarkup()}[/]");
                AnsiConsole.WriteLine();
            }
        }

        try
        {
            using var chatClient = ChatClientFactory.Create(provider, model, apiKey ?? string.Empty, baseUrl, tools);
            var repl = new ChatRepl(chatClient, tools, settings.Yes, settings.SystemPrompt);

            if (!string.IsNullOrEmpty(settings.Question))
            {
                return await repl.AskOnce(settings.Question);
            }

            return await repl.RunLoop();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OutputFormatter.WriteError(format, $"Chat error: {ex.Message}", errorCode: ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }
        finally
        {
            serviceClient?.Dispose();
        }
    }
}
