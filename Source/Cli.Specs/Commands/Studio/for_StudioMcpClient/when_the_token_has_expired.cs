// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Studio.for_StudioMcpClient;

public class when_the_token_has_expired : Specification
{
    Server _server;
    JsonNode? _answer;

    void Establish() => _server = new();

    async Task Because()
    {
        using var http = new HttpClient(_server);
        _answer = await new StudioMcpClient(http, new Uri("https://mcp.studio.test"), "acme", "client-secret").Call("studio_get_structure", [], CancellationToken.None);
    }

    [Fact] void should_exchange_the_credential_again() => _server.Tokens.ShouldEqual(2);
    [Fact] void should_answer_with_the_renewed_token() => _answer!["ok"]!.GetValue<bool>().ShouldBeTrue();

    sealed class Server : HttpMessageHandler
    {
        public int Tokens { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/.cratis/token")
            {
                Tokens++;
                return Json($"{{\"access_token\":\"token-{Tokens}\"}}");
            }

            if (request.Headers.Authorization?.Parameter == "token-1") return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var message = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            if (message["id"] is null) return new HttpResponseMessage(HttpStatusCode.Accepted);
            var result = new JsonObject();
            if (message["method"]!.GetValue<string>() == "tools/call")
            {
                result["content"] = new JsonArray();
                result["structuredContent"] = new JsonObject { ["ok"] = true };
            }

            return Json(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = message["id"]!.DeepClone(), ["result"] = result }.ToJsonString());
        }

        static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
