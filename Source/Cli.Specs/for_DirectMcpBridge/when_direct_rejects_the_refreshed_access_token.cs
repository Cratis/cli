// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_rejects_the_refreshed_access_token : given.a_bridge
{
    JsonNode _response;

    void Establish()
    {
        _direct.Answer(() => Status(HttpStatusCode.Unauthorized));
        _direct.Answer(() => Status(HttpStatusCode.Unauthorized));
    }

    async Task Because()
    {
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_refresh_only_once() => _tokens.Rejected.Count.ShouldEqual(1);
    [Fact] void should_not_retry_again() => _direct.Requests.Count.ShouldEqual(2);
    [Fact] void should_answer_the_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_an_authentication_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.AuthenticationRequired);
    [Fact] void should_tell_the_user_to_log_in() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("cratis direct login");
    [Fact] void should_not_write_either_token() => _output.ToString().ShouldNotContain("access-token");
    [Fact] void should_not_log_either_token() => _log.ToString().ShouldNotContain("access-token");
}
