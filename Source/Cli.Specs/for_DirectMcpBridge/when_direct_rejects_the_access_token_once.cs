// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_rejects_the_access_token_once : given.a_bridge
{
    const string Response = "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}";

    void Establish()
    {
        _direct.Answer(() => Status(HttpStatusCode.Unauthorized));
        _direct.Answer(() => Json(Response));
    }

    Task Because() => Forward(ListTools);

    [Fact] void should_refresh_the_rejected_token() => _tokens.Rejected.ShouldEqual(FirstToken);
    [Fact] void should_retry_with_the_refreshed_token() => _direct.Requests[1].Authorization.ShouldEqual($"Bearer {SecondToken}");
    [Fact] void should_retry_the_same_message() => _direct.Requests[1].Body.ShouldEqual(ListTools);
    [Fact] void should_relay_the_response() => _output.ToString().ShouldEqual(Response + "\n");
    [Fact] void should_not_log_either_token() => _log.ToString().ShouldNotContain("access-token");
}
