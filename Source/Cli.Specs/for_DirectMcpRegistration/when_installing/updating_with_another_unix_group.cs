// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class updating_with_another_unix_group : given.a_configuration_with_another_group
{
    void Establish() => Install(DirectMcpScope.User, ["claude"]);

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"], "team");

    [given.unix_with_multiple_groups.Fact] void should_keep_the_live_owner_group_and_mode() => ShouldKeepOwnership(_path);
    [given.unix_with_multiple_groups.Fact] void should_keep_every_backup_owner_group_with_private_mode()
    {
        foreach (var backup in Directory.GetFiles(_home, ".claude.json.*.bak")) ShouldKeepOwnership(backup, backup: true);
    }
}
