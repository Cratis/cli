// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Prologue.Configuration;
using Cratis.Prologue.Contracts;
using Cratis.Prologue.Interpretation;
using Microsoft.Extensions.AI;
using Spectre.Console;

namespace Cratis.Cli.for_InterpretPrologueCommand.given;

public class an_interpret_command : Specification
{
    protected string _folder;
    protected InterpretPrologueSettings _settings;
    protected InterpretPrologueCommand _command;
    protected IChatClientFactory _chatClients;
    protected IChatClient _client;
    protected LlmConfiguration? _global;
    protected int _globalLoads;
    protected string _noticeBeforeClient;
    protected string _noticeBeforeRequest;
    protected string _output;
    protected string _notice;
    protected int _exitCode;
    protected JsonElement _llm;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cli-241-").FullName;
        File.WriteAllText(Path.Combine(_folder, "cratis-prologue.json"), "{}");
        File.WriteAllText(Path.Combine(_folder, "http.jsonl"), CaptureFiles.Serialize(new CapturedEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            SourceKind.Http,
            new HttpCommandObserved("POST", "/api/orders", 201, string.Empty))));
        _settings = new InterpretPrologueSettings
        {
            Path = _folder,
            File = Path.Combine(_folder, "System.play"),
            Output = OutputFormats.Json,
            Yes = true
        };
        _global = new() { Kind = "openai", Model = "global-model", ApiKey = "global-secret" };
        _noticeBeforeClient = string.Empty;
        _noticeBeforeRequest = string.Empty;
        _chatClients = Substitute.For<IChatClientFactory>();
        _client = Substitute.For<IChatClient>();
        _command = new InterpretPrologueCommand(_chatClients, () =>
        {
            _globalLoads++;
            return _global;
        });
    }

    protected void Configure([StringSyntax(StringSyntaxAttribute.Json)] string json) => File.WriteAllText(Path.Combine(_folder, "cratis-prologue.json"), json);

    protected async Task Interpret()
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        var previousConsole = AnsiConsole.Console;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        _chatClients.CreateFor(Arg.Any<LlmOptions>()).Returns(_ =>
        {
            _noticeBeforeClient = error.ToString();
            return _client;
        });
        _client.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _noticeBeforeRequest = error.ToString();
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}"));
        });
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output), Ansi = AnsiSupport.No, Interactive = InteractionSupport.No });
            AnsiConsole.Console.Profile.Width = 300;
            _exitCode = await _command.ExecuteAsync(new CommandContext([], Substitute.For<IRemainingArguments>(), "interpret", null), _settings, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
            AnsiConsole.Console = previousConsole;
        }

        _output = output.ToString();
        _notice = error.ToString();
        if (_settings.ResolveOutputFormat().StartsWith(OutputFormats.Json, StringComparison.Ordinal))
        {
            using var result = JsonDocument.Parse(_output);
            _llm = result.RootElement.GetProperty("llm").Clone();
        }
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
