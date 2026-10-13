// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.given;

public class a_release_model : Specification
{
    protected const string Source = "module M\n  feature F\n    slice StateChange Record\n      event Recorded\n        value String\n";
    protected string _root;
    protected string _document;
    protected int _exitCode;
    protected string _output;
    protected string _error;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _document = Path.Combine(_root, "application.play");
        File.WriteAllText(_document, Source);
        PersistIdentities();
    }

    protected async Task Run(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = _root };
        start.Environment[UpdateChecker.DisableEnvVar] = "1";
        start.Environment["NO_COLOR"] = "1";
        start.ArgumentList.Add(typeof(ValidateScreenplayCommand).Assembly.Location);
        foreach (var argument in args)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        await process.WaitForExitAsync(deadline.Token);
        _exitCode = process.ExitCode;
        _output = await output;
        _error = await error;
    }

    void PersistIdentities()
    {
        using var output = new StringWriter();
        JsonElement opened = default;
        string? documentId = null;
        string? proposalId = null;
        using var input = new workflow_input([
            () => "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"spec\",\"version\":\"1\"}}}",
            () => "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}",
            () => Request("open-workspace", new { applicationName = "Model" }),
            () =>
            {
                opened = Result(output);
                return Request("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "documents" });
            },
            () =>
            {
                documentId = Result(output).GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString();
                return Request("propose-source", new
                {
                    expectedRevision = opened.GetProperty("revision").GetString(),
                    expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                    formatting = "CanonicalizeTouchedDocuments",
                    documents = new[] { new { operation = "replace-document", documentId, source = Source } }
                });
            },
            () =>
            {
                proposalId = Result(output).GetProperty("proposalId").GetString();
                return Request("apply", new
                {
                    expectedRevision = opened.GetProperty("revision").GetString(),
                    expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                    proposalId
                });
            }
        ]);
        ScreenplayMcpServer.Run(_root, input, output);
        Result(output).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.Exists(Path.Combine(_root, ".screenplay", "identities.json")).ShouldBeTrue();
    }

    static string Request(string name, object arguments) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "tools/call", @params = new { name, arguments } });

    static JsonElement Result(StringWriter output)
    {
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    void Destroy() => Directory.Delete(_root, true);

    sealed class workflow_input(Func<string>[] steps) : TextReader
    {
        int _index;
        int _position;
        string _line = string.Empty;

        public override int Read()
        {
            if (_position == _line.Length)
            {
                if (_index == steps.Length) return -1;
                _line = steps[_index++]() + "\n";
                _position = 0;
            }

            return _line[_position++];
        }
    }
}
