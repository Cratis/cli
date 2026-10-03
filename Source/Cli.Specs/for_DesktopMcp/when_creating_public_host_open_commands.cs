// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_DesktopMcp;

public class when_creating_public_host_open_commands : Specification
{
    ProcessStartInfo _mac;
    ProcessStartInfo _windows;
    void Because()
    {
        _mac = DesktopMcpApplications.OpenCommand(new("osx", "arm64", "home", "local", "programs"), "/Users/me/a model.mcpb");
        _windows = DesktopMcpApplications.OpenCommand(new("win", "x64", "home", "local", "programs"), "C:\\Users\\me\\a model.mcpb");
    }

    [Fact] void should_use_the_mac_public_open_tool() => _mac.FileName.ShouldEqual("/usr/bin/open");
    [Fact] void should_keep_the_bundle_path_as_one_argument() => _mac.ArgumentList.ShouldContainOnly("-a", "Claude", "/Users/me/a model.mcpb");
    [Fact] void should_use_windows_public_file_association() => _windows.UseShellExecute.ShouldBeTrue();
    [Fact] void should_not_build_a_windows_shell_command() => _windows.FileName.ShouldEqual("C:\\Users\\me\\a model.mcpb");
}
