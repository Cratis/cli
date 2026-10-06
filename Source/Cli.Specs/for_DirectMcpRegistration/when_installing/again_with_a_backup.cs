// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class again_with_a_backup : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    string _backup;

    void Establish()
    {
        Write(HomeFile(".claude.json"), Original);
        Install(DirectMcpScope.User, ["claude"]);
        _backup = Directory.GetFiles(_home, ".claude.json.*.bak").Single();
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [Fact] void should_not_make_another_backup() => Directory.GetFiles(_home, ".claude.json.*.bak").ShouldContainOnly(_backup);
    [Fact] void should_not_overwrite_the_original_backup() => File.ReadAllText(_backup).ShouldEqual(Original);
}
