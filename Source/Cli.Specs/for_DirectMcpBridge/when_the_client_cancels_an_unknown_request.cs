// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_client_cancels_an_unknown_request : given.a_bridge
{
    const string Cancel = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/cancelled\",\"params\":{\"requestId\":99}}";
    Exception? _error;

    void Establish() => _direct.AnswerWhen("notifications/cancelled", _ => Task.FromResult(Status(HttpStatusCode.Accepted)));

    async Task Because() => _error = await Catch.Exception(() => Forward(Cancel));

    [Fact] void should_forward_the_cancellation_to_direct() => _direct.Requests.Select(request => request.Body).ShouldContain(Cancel);
    [Fact] void should_not_answer_the_notification() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_stop_cleanly_at_eof() => _error.ShouldBeNull();
}
