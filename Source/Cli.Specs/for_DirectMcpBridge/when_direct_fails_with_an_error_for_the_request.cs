// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_fails_with_an_error_for_the_request : given.a_bridge
{
    const string Error = "{\"jsonrpc\":\"2.0\",\"id\":2,\"error\":{\"code\":-32602,\"message\":\"Invalid params\"}}";

    void Establish() => _direct.Answer(() => Json(HttpStatusCode.BadRequest, Error));

    Task Because() => Forward(ListTools);

    [Fact] void should_relay_directs_answer_once() => OutputLines.ShouldContainOnly(Error);
}
