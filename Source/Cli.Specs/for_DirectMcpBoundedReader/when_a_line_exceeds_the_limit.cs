// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBoundedReader;

public class when_a_line_exceeds_the_limit : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await new DirectMcpBoundedReader(new StringReader(new string('x', 20000) + "\n"), 10000).ReadLine(CancellationToken.None));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<DirectMcpMessageTooLarge>();
}
