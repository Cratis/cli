// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Build.for_McpHttpBridge;

public class when_a_request_has_no_token : given.a_bridged_server
{
    HttpResponseMessage _without;
    HttpResponseMessage _wrong;

    async Task Because()
    {
        _without = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", null);
        _wrong = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", "not-the-token");
    }

    [Fact] void should_refuse_a_request_without_a_token() => _without.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_a_request_with_another_token() => _wrong.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
