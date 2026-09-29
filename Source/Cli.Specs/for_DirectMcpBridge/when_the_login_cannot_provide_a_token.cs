// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_login_cannot_provide_a_token : given.a_bridge
{
    JsonNode _response;

    async Task Because()
    {
        using var http = new HttpClient(_direct);
        var tokens = Substitute.For<IDirectTokenProvider>();
        tokens.GetAccessToken(default!, default!, default).ReturnsForAnyArgs<string>(_ => throw new DirectAuthError("Direct session was logged out. Run 'cratis direct login'."));
        using var bridge = new DirectMcpBridge(http, tokens, _target, _issuer, _log);
        await bridge.Run(new StringReader(ListTools + "\n"), _output, CancellationToken.None);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_not_call_direct() => _direct.Requests.ShouldBeEmpty();
    [Fact] void should_answer_with_an_authentication_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.AuthenticationRequired);
    [Fact] void should_relay_the_login_guidance() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("cratis direct login");
}
