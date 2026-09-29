// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpServerSentEvents;

public class when_lines_end_with_carriage_returns : Specification
{
    readonly List<string> _events = [];

    async Task Because()
    {
        var reader = new DirectMcpBoundedReader(new StringReader("data: first\r\n\r\ndata: second\rdata: line\r\r: comment\n\ndata: unfinished"), 1024);
        await foreach (var data in DirectMcpServerSentEvents.Read(reader, CancellationToken.None))
        {
            _events.Add(data);
        }
    }

    [Fact] void should_read_each_complete_event() => _events.ShouldEqual(["first", "second\nline"]);
}
