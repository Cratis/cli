// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_client_cancels_a_request : given.a_bridge
{
    const string CallTool = "{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"tools/call\",\"params\":{\"name\":\"slow\"}}";
    const string Cancel = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/cancelled\",\"params\":{\"requestId\":7}}";
    readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _cancelledBeforeInputEnded;

    void Establish()
    {
        _direct.AnswerWhen("tools/call", async cancellationToken =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                _cancelled.TrySetResult();
            }

            return Status(HttpStatusCode.OK);
        });
        _direct.AnswerWhen("notifications/cancelled", _ => Task.FromResult(Status(HttpStatusCode.Accepted)));
    }

    async Task Because() => await Forward(new Input(this, CallTool, Cancel));

    [Fact] void should_cancel_the_forwarded_request() => _cancelledBeforeInputEnded.ShouldBeTrue();
    [Fact] void should_forward_the_cancellation_to_direct() => _direct.Requests.Select(request => request.Body).ShouldContain(Cancel);
    [Fact] void should_not_answer_the_cancelled_request() => _output.ToString().ShouldBeEmpty();

    /// <summary>Keeps standard input open until the cancelled request has stopped, as a client would.</summary>
    /// <param name="spec">The specification.</param>
    /// <param name="lines">The lines to send.</param>
    sealed class Input(when_the_client_cancels_a_request spec, params string[] lines) : TextReader
    {
        int _next;

        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (_next < lines.Length)
            {
                return lines[_next++];
            }

            spec._cancelledBeforeInputEnded = await Task.WhenAny(spec._cancelled.Task, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)) == spec._cancelled.Task;
            return null;
        }
    }
}
