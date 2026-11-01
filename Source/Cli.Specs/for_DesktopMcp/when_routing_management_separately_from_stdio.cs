// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_routing_management_separately_from_stdio : Specification
{
    string[] _original;
    string[] _desktop;
    string[] _explicitPath;
    StringWriter _error;

    void Establish()
    {
        _original = ["screenplay", "mcp", "install", "--clients", "claude"];
        _error = new();
    }

    void Because()
    {
        _desktop = DesktopMcpRoute.Normalize(_original, _error);
        _explicitPath = DesktopMcpRoute.Normalize(["screenplay", "mcp", "./install"], _error);
    }

    [Fact] void should_route_install_to_desktop_management() => _desktop.ShouldContainOnly(["screenplay", "desktop", "install", "--clients", "claude"]);
    [Fact] void should_preserve_the_callers_arguments() => _original[1].ShouldEqual("mcp");
    [Fact] void should_write_one_deprecation_line_to_standard_error() => _error.ToString().Trim().Split('\n').Length.ShouldEqual(1);
    [Fact] void should_name_the_replacement_route() => _error.ToString().ShouldContain("cratis screenplay desktop install");
    [Fact] void should_not_classify_the_normalized_route_as_protocol() => ScreenplayMcpInvocation.IsProtocolRun(_desktop).ShouldBeFalse();
    [Fact] void should_leave_an_explicit_model_path_unchanged() => _explicitPath.ShouldContainOnly(["screenplay", "mcp", "./install"]);
    [Fact] void should_still_accept_an_explicit_model_path() => ScreenplayMcpInvocation.IsProtocolRun(_explicitPath).ShouldBeTrue();

    void Destroy() => _error.Dispose();
}
