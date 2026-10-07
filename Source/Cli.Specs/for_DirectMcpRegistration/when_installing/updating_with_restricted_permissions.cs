// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class updating_with_restricted_permissions : given.a_restricted_configuration
{
    void Establish() => Install(DirectMcpScope.User, ["claude"]);

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"], "team");

    [Fact] void should_preserve_the_live_file_permissions() => ShouldRetainPermissions(_path);
    [Fact] void should_preserve_every_backup_permission()
    {
        foreach (var backup in Directory.GetFiles(_home, ".claude.json.*.bak")) ShouldRetainPermissions(backup);
    }
    [Fact] void should_update_the_registration() => File.ReadAllText(_path).ShouldContain("team");
}
