// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_detecting_clients : given.a_setup
{
    void Establish()
    {
        _settings.Clients = [];
        Directory.CreateDirectory(HomeFile(".claude"));
        Directory.CreateDirectory(HomeFile(".codex"));
    }
    async Task Because() => _result = await Run();
    [Fact] void should_install_the_detected_clients() => _result.Clients.Select(client => client.Client).ShouldEqual(["claude", "codex"]);
    [Fact] void should_login_only_once() => _logins.ShouldEqual(1);
}
