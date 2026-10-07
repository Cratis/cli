// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_routing_management_separately_from_stdio : Specification
{
    bool _desktop;
    bool _protocol;
    bool _explicitPath;
    void Because()
    {
        _desktop = DesktopMcpInvocation.IsMatch(["screenplay", "mcp", "install", "--clients", "claude"]);
        _protocol = ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "install", "--clients", "claude"]);
        _explicitPath = ScreenplayMcpInvocation.IsProtocolRun(["screenplay", "mcp", "./install"]);
    }

    [Fact] void should_route_install_to_desktop_management() => _desktop.ShouldBeTrue();
    [Fact] void should_not_pollute_a_protocol_stream() => _protocol.ShouldBeFalse();
    [Fact] void should_still_accept_an_explicit_model_path() => _explicitPath.ShouldBeTrue();
}
