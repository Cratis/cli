// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_installing_with_dry_run : given.a_personal_marketplace
{
    string _before;
    void Establish() => _before = File.ReadAllText(_marketplace);
    async Task Because() => _result = await _client.Install("not-downloaded.zip", "4.55.0", _home, true);

    [Fact] void should_preserve_the_marketplace() => File.ReadAllText(_marketplace).ShouldEqual(_before);
    [Fact] void should_not_create_a_package() => Directory.Exists(Path.Combine(_home, ".codex")).ShouldBeFalse();
    [Fact] void should_not_record_ownership() => _client.ReadOwned().ShouldBeNull();
}
