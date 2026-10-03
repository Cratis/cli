// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Cratis.Cli.for_DesktopMcp;

public class when_selecting_desktop_platforms : Specification
{
    string[] _names;
    bool _linuxSupported;
    bool _windowsArmSupported;
    void Because()
    {
        using var http = new HttpClient();
        _names = [.. new[] { ("osx", "arm64"), ("osx", "x64"), ("win", "x64") }.Select(platform => new DesktopMcpArtifacts(http, new(platform.Item1, platform.Item2, "home", "local", "programs")).Name("4.55.0", ".mcpb"))];
        _linuxSupported = new DesktopMcpPlatform("linux", "x64", "home", "local", "programs").SupportsDesktop;
        _windowsArmSupported = new DesktopMcpPlatform("win", "arm64", "home", "local", "programs").SupportsDesktop;
    }

    [Fact] void should_resolve_native_release_assets() => _names.ShouldContainOnly("screenplay-4.55.0-osx-arm64.mcpb", "screenplay-4.55.0-osx-x64.mcpb", "screenplay-4.55.0-win-x64.mcpb");
    [Fact] void should_not_claim_linux_host_support() => _linuxSupported.ShouldBeFalse();
    [Fact] void should_not_claim_windows_arm_host_support() => _windowsArmSupported.ShouldBeFalse();
}
