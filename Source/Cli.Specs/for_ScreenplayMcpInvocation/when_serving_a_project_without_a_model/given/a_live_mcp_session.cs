// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipes;
using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayMcpInvocation.when_serving_a_project_without_a_model.given;

/// <summary>
/// Runs the bundled MCP server through 'cratis screenplay mcp --project-root' over a live pipe, so a client can use
/// one response to build the next request.
/// </summary>
public class a_live_mcp_session : Specification
{
    protected string _project = null!;
    AnonymousPipeServerStream _toServer = null!;
    AnonymousPipeClientStream _serverInput = null!;
    StreamWriter _requests = null!;
    Task<int> _server = null!;
    readonly BlockingStringWriter _responses = new();
    int _nextId = 1;

    void Establish()
    {
        _project = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cratis-mcp-lazy-{Guid.NewGuid():N}")).FullName;
        File.WriteAllText(Path.Combine(_project, "README.md"), "A code-first repository.\n");
        _toServer = new AnonymousPipeServerStream(PipeDirection.Out);
        _serverInput = new AnonymousPipeClientStream(PipeDirection.In, _toServer.ClientSafePipeHandle);
        _requests = new StreamWriter(_toServer) { AutoFlush = true, NewLine = "\n" };
        var input = new StreamReader(_serverInput);
        _server = Task.Run(() => ScreenplayMcpInvocation.Run(
            null, _project, null, new ScreenplayMcpRunner(), input, _responses, TextWriter.Null, _project, _ => null));
        Send("initialize", new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "cli-specs", version = "1" } });
        _requests.WriteLine("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
    }

    protected string ModelFolder => Path.Combine(_project, "Screenplay");

    protected JsonElement Call(string tool, object? arguments = null) =>
        Send("tools/call", new { name = tool, arguments = arguments ?? new { } }).GetProperty("result");

    protected async Task<int> Stop()
    {
        await _requests.DisposeAsync();
        return await _server.WaitAsync(TimeSpan.FromSeconds(30));
    }

    JsonElement Send(string method, object parameters)
    {
        var id = _nextId++;
        _requests.WriteLine(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters }));
        return _responses.WaitForResponse(id, TimeSpan.FromSeconds(30));
    }

    void Destroy()
    {
        _serverInput.Dispose();
        _toServer.Dispose();
        Directory.Delete(_project, recursive: true);
    }

    sealed class BlockingStringWriter : StringWriter
    {
        readonly Lock _lock = new();
        readonly Dictionary<int, JsonElement> _responses = [];
        readonly SemaphoreSlim _written = new(0);
        string _pending = string.Empty;

        public JsonElement WaitForResponse(int id, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                lock (_lock)
                {
                    if (_responses.Remove(id, out var response)) return response;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero || !_written.Wait(remaining))
                {
                    throw new TimeoutException($"The MCP server did not answer request {id}.");
                }
            }
        }

        public override void Write(char value) => Write(value.ToString());

        public override void Write(string? value)
        {
            if (value is null) return;
            lock (_lock)
            {
                _pending += value;
                int newline;
                while ((newline = _pending.IndexOf('\n')) >= 0)
                {
                    var line = _pending[..newline];
                    _pending = _pending[(newline + 1)..];
                    if (line.Length == 0) continue;
                    using var document = JsonDocument.Parse(line);
                    if (document.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number)
                    {
                        _responses[id.GetInt32()] = document.RootElement.Clone();
                    }
                }
            }

            _written.Release();
        }

        public override void WriteLine(string? value) => Write($"{value}\n");

        protected override void Dispose(bool disposing)
        {
            if (disposing) _written.Dispose();
            base.Dispose(disposing);
        }
    }
}
