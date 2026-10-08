// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpClients.when_rendering_arguments;

public class without_a_tenant : Specification
{
    IReadOnlyList<string> _arguments;

    void Because() => _arguments = DirectMcpClients.Arguments("https://direct.example", null);

    [Fact] void should_pin_no_tenant_explicitly() => string.Join(' ', _arguments).ShouldEqual("direct mcp --url https://direct.example --no-tenant");
}
