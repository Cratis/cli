// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReopenClosedStreamCommand;

public class when_confirming_the_repair : given.a_reopen_closed_stream_command
{
    string _prompt;

    void Because() => _prompt = _command.Prompt(_settings);

    [Fact] void should_identify_the_store_and_namespace() => _prompt.ShouldContain("event store 'store', namespace 'tenant'");
    [Fact] void should_identify_the_sequence() => _prompt.ShouldContain("sequence 'outbox'");
    [Fact] void should_identify_the_exact_dimensions() => _prompt.ShouldContain("event source id: order-42, event source type: Order, event stream type: Accounting, event stream id: 2026");
    [Fact] void should_show_the_reason() => _prompt.ShouldContain("Closed the wrong order");
}
