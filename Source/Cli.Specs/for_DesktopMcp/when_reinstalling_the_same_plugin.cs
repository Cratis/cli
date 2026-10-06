// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_reinstalling_the_same_plugin : given.a_personal_marketplace
{
    string _before;
    async Task Establish()
    {
        await _client.Install(_artifact, "4.55.0", _home, false);
        _before = await File.ReadAllTextAsync(_marketplace);
    }
    async Task Because() => _result = await _client.Install(_artifact, "4.55.0", _home, false);

    [Fact] void should_not_rewrite_the_marketplace() => File.ReadAllText(_marketplace).ShouldEqual(_before);
    [Fact] void should_not_duplicate_packages() => Directory.GetDirectories(Path.Combine(_home, ".codex/plugins/cratis-screenplay")).Length.ShouldEqual(1);
    [Fact] void should_report_the_unchanged_source() => _result.ShouldContain("Source is unchanged");
}
