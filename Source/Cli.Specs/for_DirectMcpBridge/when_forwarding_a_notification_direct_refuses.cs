// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_forwarding_a_notification_direct_refuses : given.a_bridge
{
    void Establish() => _direct.Answer(() => Json(HttpStatusCode.BadRequest, "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32600,\"message\":\"Bad Request\"}}"));

    Task Because() => Forward(Initialized);

    [Fact] void should_write_nothing_back() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_log_the_failure() => _log.ToString().ShouldContain("400");
}
