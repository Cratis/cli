// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBoundedReader;

public class when_reading_to_the_end : Specification
{
    readonly string _text = new('x', 20000);
    string _read;

    async Task Because() => _read = await new DirectMcpBoundedReader(new StringReader(_text), 20000).ReadToEnd(CancellationToken.None);

    [Fact] void should_read_everything_within_the_limit() => _read.ShouldEqual(_text);
}
