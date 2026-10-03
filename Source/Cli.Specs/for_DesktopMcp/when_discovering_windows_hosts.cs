// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_discovering_windows_hosts : given.a_personal_marketplace
{
    string _claude;
    string _chatgpt;
    void Establish()
    {
        _platform = new("win", "x64", _home, _home, _home);
        Directory.CreateDirectory(Path.Combine(_home, "Microsoft", "WindowsApps"));
        File.WriteAllText(Path.Combine(_home, "Microsoft", "WindowsApps", "Claude.exe"), "alias");
        File.WriteAllText(Path.Combine(_home, "Microsoft", "WindowsApps", "ChatGPT.exe"), "alias");
    }
    void Because()
    {
        _claude = DesktopMcpApplications.Find(_platform, "Claude")!;
        _chatgpt = DesktopMcpApplications.Find(_platform, "ChatGPT")!;
    }

    [Fact] void should_detect_the_claude_application_alias() => _claude.ShouldEqual(Path.Combine(_home, "Microsoft", "WindowsApps", "Claude.exe"));
    [Fact] void should_detect_the_chatgpt_application_alias() => _chatgpt.ShouldEqual(Path.Combine(_home, "Microsoft", "WindowsApps", "ChatGPT.exe"));
}
