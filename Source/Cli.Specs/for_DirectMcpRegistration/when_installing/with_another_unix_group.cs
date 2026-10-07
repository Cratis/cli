// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_another_unix_group : given.a_configuration_with_another_group
{
    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [given.unix_with_multiple_groups.Fact] void should_start_with_a_group_different_from_the_creation_group() => (_originalGroup != _creationGroup).ShouldBeTrue();
    [given.unix_with_multiple_groups.Fact] void should_keep_the_live_owner_group_and_mode() => ShouldKeepOwnership(_path);
    [given.unix_with_multiple_groups.Fact] void should_keep_the_backup_owner_group_and_mode() => ShouldKeepOwnership(Directory.GetFiles(_home, ".claude.json.*.bak").Single());
}
