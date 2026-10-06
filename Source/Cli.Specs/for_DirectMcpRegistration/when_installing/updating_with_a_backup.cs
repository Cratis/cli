// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class updating_with_a_backup : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    string _backup;
    byte[] _installed;

    void Establish()
    {
        Write(HomeFile(".claude.json"), Original);
        Install(DirectMcpScope.User, ["claude"]);
        _backup = Directory.GetFiles(_home, ".claude.json.*.bak").Single();
        _installed = File.ReadAllBytes(HomeFile(".claude.json"));
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"], "team");

    [Fact] void should_preserve_the_first_backup() => File.ReadAllText(_backup).ShouldEqual(Original);
    [Fact] void should_back_up_the_configuration_before_the_update() => File.ReadAllBytes(Directory.GetFiles(_home, ".claude.json.*.bak").Single(path => path != _backup)).ShouldEqual(_installed);
}
