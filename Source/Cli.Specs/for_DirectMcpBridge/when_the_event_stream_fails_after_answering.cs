// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_event_stream_fails_after_answering : given.a_bridge
{
    const string Response = "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}";

    void Establish() => _direct.Answer(() =>
    {
        var content = new StreamContent(new FailingAfter(Encoding.UTF8.GetBytes("data: " + Response + "\n\n")));
        content.Headers.ContentType = new("text/event-stream");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    });

    Task Because() => Forward(ListTools);

    [Fact] void should_answer_the_request_exactly_once() => OutputLines.ShouldContainOnly(Response);

    sealed class FailingAfter(byte[] content) : MemoryStream(content)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            Position < Length ? base.Read(buffer, offset, count) : throw new IOException("The connection was reset.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Position < Length ? base.ReadAsync(buffer, cancellationToken) : throw new IOException("The connection was reset.");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Position < Length ? base.ReadAsync(buffer, offset, count, cancellationToken) : throw new IOException("The connection was reset.");
    }
}
