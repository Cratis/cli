// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge.given;

public class a_bridge : Specification
{
    protected const string FirstToken = "first-access-token";
    protected const string SecondToken = "second-access-token";
    protected const string Initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\"}}";
    protected const string ListTools = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}";
    protected const string Initialized = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}";

    private protected static readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    private protected static readonly Uri _issuer = new("https://identity.example/");
    private protected Tokens _tokens;
    private protected Direct _direct;
    protected StringWriter _output;
    protected StringWriter _log;
    HttpClient _http;
    DirectMcpBridge _bridge;

    void Establish()
    {
        _tokens = new();
        _direct = new();
        _output = new();
        _log = new();
        _http = new(_direct);

        // Requests log concurrently; match Console.Error's synchronized writer without racing StringWriter's buffer.
        _bridge = new(_http, _tokens, _target, _issuer, TextWriter.Synchronized(_log));
    }

    protected IReadOnlyList<string> OutputLines => _output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    protected Task Forward(params string[] lines) =>
        _bridge.Run(new StringReader(string.Join('\n', lines) + "\n"), _output, CancellationToken.None);

    protected Task Forward(TextReader input) => _bridge.Run(input, _output, CancellationToken.None);

    protected Task Forward(TextReader input, CancellationToken cancellationToken) => _bridge.Run(input, _output, cancellationToken);

    protected async Task ForwardWithLimit(int limit, params string[] lines)
    {
        using var bridge = new DirectMcpBridge(_http, _tokens, _target, _issuer, TextWriter.Synchronized(_log), limit);
        await bridge.Run(new StringReader(string.Join('\n', lines) + "\n"), _output, CancellationToken.None);
    }

    protected static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected static HttpResponseMessage Events(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/event-stream") };

    protected static HttpResponseMessage Status(HttpStatusCode status) => new(status) { Content = new StringContent(string.Empty) };

    protected static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    void Destroy()
    {
        _bridge.Dispose();
        _http.Dispose();
        _output.Dispose();
        _log.Dispose();
    }

    internal sealed record Received(HttpMethod Method, Uri Uri, string? Authorization, string Accept, string? ContentType, string? ProtocolVersion, string? SessionId, string Body);

    internal sealed class Direct : HttpMessageHandler
    {
        readonly Queue<Func<HttpResponseMessage>> _responses = new();
        readonly List<(string Fragment, Func<CancellationToken, Task<HttpResponseMessage>> Respond)> _routes = [];

        public List<Received> Requests { get; } = [];

        public void Answer(Func<HttpResponseMessage> response) => _responses.Enqueue(response);

        /// <summary>Answers every request whose body contains a fragment, whatever order requests arrive in.</summary>
        /// <param name="fragment">The body fragment.</param>
        /// <param name="respond">The response.</param>
        public void AnswerWhen(string fragment, Func<CancellationToken, Task<HttpResponseMessage>> respond) => _routes.Add((fragment, respond));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var received = new Received(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.ToString(),
                string.Join(", ", request.Headers.Accept.Select(value => value.MediaType)),
                request.Content?.Headers.ContentType?.MediaType,
                request.Headers.TryGetValues("MCP-Protocol-Version", out var versions) ? versions.Single() : null,
                request.Headers.TryGetValues("Mcp-Session-Id", out var sessions) ? sessions.Single() : null,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            lock (Requests)
            {
                Requests.Add(received);
            }

            var body = received.Body;
            foreach (var (fragment, respond) in _routes)
            {
                if (body.Contains(fragment, StringComparison.Ordinal))
                {
                    return await respond(cancellationToken);
                }
            }

            lock (_responses)
            {
                return _responses.Dequeue()();
            }
        }
    }

    internal sealed class Tokens : IDirectTokenProvider
    {
        public string Current { get; set; } = FirstToken;
        public string Refreshed { get; set; } = SecondToken;
        public List<string> Rejected { get; } = [];
        public Exception? Failure { get; set; }

        public Task<string> GetAccessToken(DirectTarget target, Uri issuer, CancellationToken cancellationToken) =>
            Failure is null ? Task.FromResult(Current) : Task.FromException<string>(Failure);

        public Task<string> RefreshAccessToken(DirectTarget target, Uri issuer, string rejected, CancellationToken cancellationToken)
        {
            Rejected.Add(rejected);
            Current = Refreshed;
            return Task.FromResult(Current);
        }
    }
}
