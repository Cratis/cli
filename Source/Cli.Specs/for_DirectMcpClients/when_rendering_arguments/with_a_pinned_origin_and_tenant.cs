// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpClients.when_rendering_arguments;

public class with_a_pinned_origin_and_tenant : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = DirectMcpClients.Arguments("https://Direct.Example.com", "team");

    [Fact] void should_pin_the_normalized_origin_and_the_tenant() => string.Join(' ', _arguments).ShouldEqual("direct mcp --url https://direct.example.com --tenant team");
}
